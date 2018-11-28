using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DoubleDispatchSample.DomainService
{
    public interface IPurchaseOrderService
    {
        bool WouldAddBeUnderLimit(PurchaseOrder order, LineItem newItem);
        bool WouldUpdateBeUnderLimit(int purchaseOrderId, LineItem existingItem, decimal newCost);
    }

    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;

        public PurchaseOrderService(IPurchaseOrderRepository purchaseOrderRepository)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
        }
        public bool WouldAddBeUnderLimit(PurchaseOrder order, LineItem newItem)
        {
            return order.Items.Sum(i => i.Cost) + newItem.Cost <= order.SpendLimit;
        }

        public bool WouldUpdateBeUnderLimit(int purchaseOrderId, LineItem existingItem, decimal newCost)
        {
            var po = _purchaseOrderRepository.GetById(purchaseOrderId);
            // check for null, check if item belongs to PO
            return po.Items.Sum(i => i.Cost) + (newCost - existingItem.Cost) <= po.SpendLimit;
        }
    }

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

        public bool TryAddItem(LineItem item, IPurchaseOrderService poService)
        {
            if (poService.WouldAddBeUnderLimit(this, item))
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

        public bool TryUpdateCost(decimal cost, IPurchaseOrderService poService)
        {
            if (poService.WouldUpdateBeUnderLimit(PurchaseOrderId, this, cost))
            {
                Cost = cost;
                return true;
            }
            return false;
        }
    }

    public interface IPurchaseOrderRepository
    {
        void Add(PurchaseOrder purchaseOrder);
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

    public class DomainServiceTest
    {
        private IPurchaseOrderRepository _purchaseOrderRepo;
        private IPurchaseOrderService _purchaseOrderService;

        public DomainServiceTest()
        {
            _purchaseOrderRepo = new InMemoryPurchaseOrderRepository();
            _purchaseOrderService = new PurchaseOrderService(_purchaseOrderRepo);
        }

        [Fact]
        public void AddItemAboveLimitReturnsFalse()
        {
            var po = new PurchaseOrder() { SpendLimit = 100 };
            _purchaseOrderRepo.Add(po);

            po.TryAddItem(new LineItem(50), _purchaseOrderService);
            var item = new LineItem(51);
            Assert.False(po.TryAddItem(item, _purchaseOrderService));
        }

        [Fact]
        public void UpdateItemAboveLimitReturnsFalse()
        {
            var po = new PurchaseOrder() { SpendLimit = 100 };
            _purchaseOrderRepo.Add(po);
            po.TryAddItem(new LineItem(50), _purchaseOrderService);
            var item = new LineItem(25);
            po.TryAddItem(item, _purchaseOrderService);

            Assert.False(item.TryUpdateCost(51, _purchaseOrderService));
        }
    }
}
