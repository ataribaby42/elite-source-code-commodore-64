using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace C64EliteDials
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        byte[] dials;
        int cursorX = 0;
        int cursorY = 0;

        private byte[] FileToByteArray(string fileName)
        {
            byte[] buff;
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            long numBytes = new FileInfo(fileName).Length;
            buff = br.ReadBytes((int)numBytes);
            return buff;
        }

        private void Point(byte[] bitmap, int x, int y, int color)
        {
            int index = (y * (160 * 3)) + (x * 3);

            if (color == 0)
            {
                bitmap[index] = 0;
                bitmap[index + 1] = 0;
                bitmap[index + 2] = 0;
            }
            else if (color == 1)
            {
                bitmap[index] = 255;
                bitmap[index + 1] = 0;
                bitmap[index + 2] = 0;
            }
            else if (color == 2)
            {
                bitmap[index] = 0;
                bitmap[index + 1] = 255;
                bitmap[index + 2] = 0;
            }
            else if (color == 3)
            {
                bitmap[index] = 0;
                bitmap[index + 1] = 0;
                bitmap[index + 2] = 255;
            }
            else if (color == 4)
            {
                bitmap[index] = 255;
                bitmap[index + 1] = 255;
                bitmap[index + 2] = 255;
            }
        }

        private void ProcessDials()
        {
            PixelFormat pf = PixelFormats.Rgb24;
            int width = 160;
            int height = 56;
            int rawStride = (width * pf.BitsPerPixel + 7) / 8;
            byte[] rawImage = new byte[rawStride * height];

            for (int i = 0; i < rawImage.Length; i++)
            {
                rawImage[i] = 0;
            }

            // 160x56

            int x = 0;
            int y = 0;
            int row = 0;
            int col = 0;

            for (int i = 0; i < 40 * 7 * 8; i++)
            {
                byte pixels = dials[i];
                string bits = Convert.ToString(pixels, 2).PadLeft(8, '0');
                int pixel1 = Convert.ToInt32(bits.Substring(0, 2), 2);
                int pixel2 = Convert.ToInt32(bits.Substring(2, 2), 2);
                int pixel3 = Convert.ToInt32(bits.Substring(4, 2), 2);
                int pixel4 = Convert.ToInt32(bits.Substring(6, 2), 2);

                Point(rawImage, x, y, pixel1);
                x++;
                Point(rawImage, x, y, pixel2);
                x++;
                Point(rawImage, x, y, pixel3);
                x++;
                Point(rawImage, x, y, pixel4);
                x -= 3;
                y++;
                row++;

                if (row == 8)
                {
                    col++;
                    row = 0;
                    y -= 8;
                    x += 4;

                    if (col == 40)
                    {
                        col = 0;
                        x = 0;
                        y += 8;
                    }
                }
            }

            Point(rawImage, cursorX, cursorY, 4);

            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, pf, null, rawImage, rawStride);

            myImage.Source = bitmap;
        }

        public MainWindow()
        {
            InitializeComponent();

            //dials = FileToByteArray("C.CODIALS.bin");
            dials = FileToByteArray("C.CODIALSNEW.bin");
            
            ProcessDials();
        }

        private void SetCursorPixel(string color)
        {
            int image_char_width = 40;
            int x = cursorX;
            int y = cursorY;

            // 1. Identify character grid location
            int char_x = x / 4;
            int char_y = y / 8;

            // 2. Local position inside the 4x8 character
            int local_x = x % 4;
            int local_y = y % 8;

            // 3. Index of the character in the buffer
            int char_index = (char_y * image_char_width) + char_x;

            // 4. Byte offset to the specific row in the character
            int byte_offset = char_index * 8 + local_y;

            // 5. Bit position of the 2-bit pixel in the byte (MSB = leftmost pixel)
            int bit_position = (3 - local_x) * 2;

            byte pixelByte = dials[byte_offset];
            string bits = Convert.ToString(pixelByte, 2).PadLeft(8, '0');

            if (bit_position == 6)
            {
                bits = color + bits.Substring(2, 6);
            }
            else if (bit_position == 4)
            {
                bits = bits.Substring(0, 2) + color + bits.Substring(4, 4);
            }
            else if (bit_position == 2)
            {
                bits = bits.Substring(0, 4) + color + bits.Substring(6, 2);
            }
            if (bit_position == 0)
            {
                bits = bits.Substring(0, 6) + color;
            }

            dials[byte_offset] = Convert.ToByte(bits, 2);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                cursorX = Math.Min(159, cursorX + 1);

                ProcessDials();
            }
            else if (e.Key == Key.Left)
            {
                cursorX = Math.Max(0, cursorX - 1);

                ProcessDials();
            }
            if (e.Key == Key.Down)
            {
                cursorY = Math.Min(55, cursorY + 1);

                ProcessDials();
            }
            else if (e.Key == Key.Up)
            {
                cursorY = Math.Max(0, cursorY - 1);

                ProcessDials();
            }
            else if (e.Key == Key.Enter)
            {
                SetCursorPixel("00");
                ProcessDials();
            }
            else if (e.Key == Key.D1)
            {
                SetCursorPixel("01");
                ProcessDials();
            }
            else if (e.Key == Key.D2)
            {
                SetCursorPixel("10");
                ProcessDials();
            }
            else if (e.Key == Key.D3)
            {
                SetCursorPixel("11");
                ProcessDials();
            }
            else if (e.Key == Key.S)
            {
                File.WriteAllBytes("C.CODIALSNEW.bin", dials);
            }
        }
    }
}