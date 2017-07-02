using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FloodFillAlgorithm
{
    public partial class Form : System.Windows.Forms.Form
    {
        Bitmap Bmp { get; }

        public Form()
        {
            InitializeComponent();
            DoubleBuffered = true;

            Bmp = new Bitmap(Width, Height);
            using (var g = Graphics.FromImage(Bmp))
            {
                g.Clear(Color.White);
            }
            Paint += OnPaint;
            MouseDown += OnDown;
        }

        void OnPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(Bmp, 0, 0);
        }

        void OnDown(object sender, MouseEventArgs e)
        {
            var timer = Stopwatch.StartNew();
            var startPos = new Point(e.X, e.Y);
            switch (e.Button)
            {
            case MouseButtons.Left: FloodFill(startPos, Color.SteelBlue);   break;
            case MouseButtons.Right:FloodFillSimple(startPos, Color.Coral); break;
            }
            timer.Stop();

            Invalidate();
            MessageBox.Show($"{timer.ElapsedMilliseconds}ms");
        }

        private void FloodFill(Point pos, Color paintColor)
        {
            // 바운더리 체크
            if (pos.X < 0 || pos.X >= Bmp.Width || pos.Y < 0 || pos.Y >= Bmp.Height)
                return;

            // 같은 색상이면 생략
            var prevColor = Bmp.GetPixel(pos.X, pos.Y);
            if (prevColor.ToArgb() == paintColor.ToArgb())
                return;

            // GetPixel()함수가 굉장히 느리기 때문에 byte배열로 복사해서 작업한다.
            // unsafe를 쓰는게 제일 빠르지만, Marshal.Copy()로 해도 크게 차이 없다.(복사비용 두번 생길뿐)
            var data = Bmp.LockBits(new Rectangle(0, 0, Bmp.Width, Bmp.Height), ImageLockMode.ReadWrite, Bmp.PixelFormat);
            var pixels = new byte[data.Stride * Bmp.Height];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

            // 중복체크를 피하기 위해 캐시공간을 마련한다.
            var checks = new bool[Bmp.Width, Bmp.Height];

            // 재귀호출은 스택오버플로우 위험이 있으므로 스택 자료구조를 사용한다.
            var stack = new Stack<Point>(Bmp.Width * Bmp.Height);

            // 체크를 간편화하기 위한 람다함수 정의
            // VS2017이면 지역함수를 이용하면 더 보기 좋겠지만, VS2015에서 빌드해야하니 람다 이용
            Action<int, int> CheckAndPushFunc = (int x, int y) =>
            {
                if (x < 0 || x >= Bmp.Width ||
                    y < 0 || y >= Bmp.Height)
                    return;

                if (checks[x, y])
                    return;

                checks[x, y] = true;
                stack.Push(new Point(x, y));
            };

            // 변수 캐싱(Color.R 등의 프로퍼티는 호출비용이 약간 있는듯하다.)
            var bytesPerPixel = Image.GetPixelFormatSize(Bmp.PixelFormat) / 8;
            var prevR = prevColor.R; var paintR = paintColor.R;
            var prevG = prevColor.G; var paintG = paintColor.G;
            var prevB = prevColor.B; var paintB = paintColor.B;

            // Flood-fill 알고리즘 구현부
            stack.Push(pos);
            while (stack.Any())
            {
                var curPixel = stack.Pop();
                var curX = curPixel.X * bytesPerPixel;
                var curY = curPixel.Y * data.Stride;

                // 처음 클릭한 색상과 같은 색상이면(즉, 바꿔야할 색상이면)
                if (prevB == pixels[curY + curX] &&
                    prevG == pixels[curY + curX + 1] &&
                    prevR == pixels[curY + curX + 2])
                {
                    // 바꾸려는 색상으로 대입해주고
                    pixels[curY + curX] = paintB;
                    pixels[curY + curX + 1] = paintG;
                    pixels[curY + curX + 2] = paintR;

                    // 상하좌우 픽셀을 스택에 넣어 위 작업을 재귀적으로 수행한다.
                    CheckAndPushFunc(curPixel.X - 1, curPixel.Y);
                    CheckAndPushFunc(curPixel.X + 1, curPixel.Y);
                    CheckAndPushFunc(curPixel.X, curPixel.Y - 1);
                    CheckAndPushFunc(curPixel.X, curPixel.Y + 1);
                }
            }

            // 작업이 완료된 byte배열을 다시 원래의 비트맵공간에 덮어쓴다.
            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            Bmp.UnlockBits(data);
        }

        private void FloodFillSimple(Point pos, Color paintColor)
        {
            var prevColor = Bmp.GetPixel(pos.X, pos.Y);
            if (prevColor.ToArgb().Equals(paintColor.ToArgb()))
                return;

            var pixels = new Stack<Point>();
            pixels.Push(pos);
            while (pixels.Count > 0)
            {
                var curPixel = pixels.Pop();
                if (curPixel.X < 0 || curPixel.X >= Bmp.Width ||
                    curPixel.Y < 0 || curPixel.Y >= Bmp.Height)
                    continue;

                if (Bmp.GetPixel(curPixel.X, curPixel.Y) == prevColor)
                {
                    Bmp.SetPixel(curPixel.X, curPixel.Y, paintColor);

                    pixels.Push(new Point(curPixel.X - 1, curPixel.Y));
                    pixels.Push(new Point(curPixel.X + 1, curPixel.Y));
                    pixels.Push(new Point(curPixel.X, curPixel.Y - 1));
                    pixels.Push(new Point(curPixel.X, curPixel.Y + 1));
                }
            }
        }
    }
}
