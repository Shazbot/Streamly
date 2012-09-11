using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace LeStreamsFace
{
    public class GameIconAdorner : Adorner
    {
        private ImageBrush brush { get; set; }

        public GameIconAdorner(UIElement adornedElement, ImageBrush brush)

            : base(adornedElement)
        {
            this.brush = brush;
            IsHitTestVisible = false;
            Opacity = 0.5;
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            //            brush.RelativeTransform = new ScaleTransform(1.5, 1.5) { CenterX = 0.5, CenterY = 0.5 };

            drawingContext.DrawRectangle(brush, null, new Rect(DesiredSize));
        }
    }
}