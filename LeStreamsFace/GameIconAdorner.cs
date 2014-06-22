using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace LeStreamsFace
{
    public class GameIconAdorner : Adorner
    {
        private ImageBrush Brush { get; set; }

        public GameIconAdorner(UIElement adornedElement, ImageBrush brush)
            : base(adornedElement)
        {
            this.Brush = brush;
            IsHitTestVisible = false;
            Opacity = 0.5;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            //            Brush.RelativeTransform = new ScaleTransform(1.5, 1.5) { CenterX = 0.5, CenterY = 0.5 };
            drawingContext.DrawRectangle(Brush, null, new Rect(DesiredSize));
        }
    }
}