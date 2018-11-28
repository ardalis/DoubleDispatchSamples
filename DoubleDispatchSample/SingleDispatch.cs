using System;
using System.Text;
using Xunit;

namespace DoubleDispatchSample.SingleDispatch
{
    public class SingleDispatchTest
    {
        public class Pen { }
        public class Figure
        {
            private readonly StringBuilder _stringBuilder;

            public Figure(StringBuilder stringBuilder)
            {
                _stringBuilder = stringBuilder;
            }
            public void Draw(Pen pen)
            {
                _stringBuilder.AppendLine("Figure drawn in pen.");
            }
            public void Draw(Object something)
            {
                _stringBuilder.AppendLine("Figure drawn with something.");
            }
        }

        [Fact]
        public void Test()
        {
            var sb = new StringBuilder();
            var figure = new Figure(sb);

            figure.Draw(new Pen());
            figure.Draw(figure);

            var result = sb.ToString();

            Assert.Equal(@"Figure drawn in pen." + Environment.NewLine +
                          "Figure drawn with something." + Environment.NewLine, result);

        }
    }
}
