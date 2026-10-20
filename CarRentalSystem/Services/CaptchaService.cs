using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CarRentalSystem.Services
{
    public static class CaptchaService
    {
        public static string GenerateText(int length)
        {
            string allowchar = "A,B,C,D,E,F,G,H,K,L,M,N,P,Q,R,S,T,U,V,W,X,Y,Z,1,2,3,4,5,6,7,8,9";
            string[] ar = allowchar.Split(',');
            string pwd = "";
            Random r = new Random();

            for (int i = 0; i < length; i++)
            {
                pwd += ar[r.Next(0, ar.Length)];
            }
            return pwd;
        }

        public static BitmapSource CreateCaptchaImage(string text, int width, int height)
        {
            Random rand = new Random();

            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(
                    new SolidColorBrush(Color.FromRgb(245, 246, 249)),
                    null,
                    new Rect(0, 0, width, height)
                );

                Color[] noiseColors = { Colors.LightGray, Colors.Gainsboro, Colors.Silver };
                for (int i = 0; i < 15; i++)
                {
                    Pen noisePen = new Pen(new SolidColorBrush(noiseColors[rand.Next(0, noiseColors.Length)]), rand.Next(1, 2));
                    drawingContext.DrawLine(
                        noisePen,
                        new Point(rand.Next(0, width), rand.Next(0, height)),
                        new Point(rand.Next(0, width), rand.Next(0, height))
                    );
                }

                Color[] textColors = { Colors.DarkBlue, Colors.DarkRed, Colors.DarkGreen, Colors.Purple, Colors.DarkSlateGray };
                string[] fontFamilies = { "Arial", "Verdana", "Times New Roman", "Georgia" };

                double charWidth = (double)width / (text.Length + 1);

                for (int i = 0; i < text.Length; i++)
                {
                    FormattedText formattedText = new FormattedText(
                        text[i].ToString(),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(fontFamilies[rand.Next(0, fontFamilies.Length)]),
                        rand.Next(22, 28),
                        new SolidColorBrush(textColors[rand.Next(0, textColors.Length)])
                    );

                    
                    double posX = (i + 0.5) * charWidth;
                    double posY = (height - formattedText.Height) / 2;

                    TransformGroup transformGroup = new TransformGroup();
                    transformGroup.Children.Add(new RotateTransform(rand.Next(-20, 20), formattedText.Width / 2, formattedText.Height / 2));
                    transformGroup.Children.Add(new TranslateTransform(posX, posY));

                    drawingContext.PushTransform(transformGroup);
                    drawingContext.DrawText(formattedText, new Point(0, 0));
                    drawingContext.Pop(); 
                }

                for (int i = 0; i < 20; i++)
                {
                    drawingContext.DrawGeometry(
                        new SolidColorBrush(Colors.Gray),
                        null,
                        new CombinedGeometry(GeometryCombineMode.Union,
                            new EllipseGeometry(new Point(rand.Next(0, width), rand.Next(0, height)), 1, 1),
                            new EllipseGeometry(new Point(rand.Next(0, width), rand.Next(0, height)), 1, 1)
                        )
                    );
                }
            }

            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);

            return renderTargetBitmap;
        }
    }
}
