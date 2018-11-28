using System;
using System.Collections.Generic;
using System.Linq;

namespace DoubleDispatchSample.Aggregate
{
    public class PurchaseOrder // aggregate root
    {
        public int Id { get; set; }
        private List<LineItem> _items { get; } = new List<LineItem>();
        public IEnumerable<LineItem> Items => _items.ToList();

        public decimal SpendLimit { get; set; }

        public bool CheckLimit(LineItem item, decimal newValue)
        {
            var currentSum = Items.Sum(i => i.Cost);
            decimal difference = newValue - item.Cost;

            return currentSum + difference <= SpendLimit;
        }

        public bool CheckLimit(LineItem newItem)
        {
            return Items.Sum(i => i.Cost) + newItem.Cost <= SpendLimit;
        }

        public bool TryAddItem(LineItem item)
        {
            if (CheckLimit(item))
            {
                _items.Add(item);
                return true;
            }
            return false;
        }
    }

    public class LineItem
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; } // avoid having circular reference between aggregate and children
        public LineItem(decimal cost)
        {
            Cost = cost;
        }
        public decimal Cost { get; private set; }

        public bool TryUpdateCost(decimal cost, PurchaseOrder parent)
        {
            if (parent.Id != PurchaseOrderId) throw new Exception("Incorrect parent PO.");
            // check if new cost would exceed PO
            if (parent.CheckLimit(this, cost))
            {
                Cost = cost;
                return true;
            }
            return false;
        }

        // alternate implementation
        public bool TryUpdateCost(decimal cost, IPurchaseOrderRepository purchaseOrderRepository)
        {
            var parent = purchaseOrderRepository.GetById(PurchaseOrderId);
            // check if new cost would exceed PO
            if (parent.CheckLimit(this, cost))
            {
                Cost = cost;
                return true;
            }
            return false;
        }
    }

    public interface IPurchaseOrderRepository
    {
        PurchaseOrder GetById(int id);
    }

    public class InMemoryPurchaseOrderRepository : IPurchaseOrderRepository
    {
        private Dictionary<int, PurchaseOrder> _collection = new Dictionary<int, PurchaseOrder>();
        public void Add(PurchaseOrder purchaseOrder)
        {
            if (!_collection.ContainsKey(purchaseOrder.Id))
            {
                _collection.Add(purchaseOrder.Id, purchaseOrder);
            }
        }

        public PurchaseOrder GetById(int id)
        {
            if (!_collection.ContainsKey(id)) return null;
            return _collection[id];
        }
    }

    public class AggregateTest
    {
        [Fact]
        public void AddItemAboveLimitReturnsFalse()
        {
            var po = new PurchaseOrder() { SpendLimit = 100 };
            po.TryAddItem(new LineItem(50));
            var item = new LineItem(51);
            Assert.False(po.TryAddItem(item));
        }

        [Fact]
        public void UpdateItemAboveLimitReturnsFalse()
        {
            var po = new PurchaseOrder() { SpendLimit = 100 };
            po.TryAddItem(new LineItem(50));
            var item = new LineItem(25);
            po.TryAddItem(item);

            Assert.False(item.TryUpdateCost(51, po));
        }

        [Fact]
        public void UpdateItemAboveLimitReturnsFalseWithRepository()
        {
            var repo = new InMemoryPurchaseOrderRepository();

            var po = new PurchaseOrder() { SpendLimit = 100 };
            repo.Add(po);

            po.TryAddItem(new LineItem(50));
            var item = new LineItem(25);
            po.TryAddItem(item);

            Assert.False(item.TryUpdateCost(51, repo)); // no longer possible to use wrong PO
        }
    }
}
