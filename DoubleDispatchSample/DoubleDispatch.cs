using System;
using System.Text;
using Xunit;

namespace DoubleDispatchSample.DoubleDispatch
{
    public abstract class Pen
    {
        public abstract void Draw(StringBuilder sb);
    }

    public class RedPen : Pen
    {
        public override void Draw(StringBuilder sb)
        {
            sb.Append("in red pen.");
        }
    }

    public class BlackPen : Pen
    {
        public override void Draw(StringBuilder sb)
        {
            sb.Append("in black pen.");
        }
    }

    public class Figure
    {
        private readonly StringBuilder _stringBuilder;

        public Figure(StringBuilder stringBuilder)
        {
            _stringBuilder = stringBuilder;
        }
        public void Draw(Pen pen)
        {
            _stringBuilder.Append("Figure drawn ");
            pen.Draw(_stringBuilder);
            _stringBuilder.AppendLine();
        }
    }

    public class DoubleDispatchTest
    {
        [Fact]
        public void Test()
        {
            var sb = new StringBuilder();
            var figure = new Figure(sb);

            figure.Draw(new RedPen());
            figure.Draw(new BlackPen());

            var result = sb.ToString();

            Assert.Equal(@"Figure drawn in red pen." + Environment.NewLine +
                          "Figure drawn in black pen." + Environment.NewLine, result);

        }
    }
}
