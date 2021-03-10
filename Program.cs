using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab3_steg
{
    public class Pixel
    {
        public Pixel(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public Pixel(Color color)
        {
            R = color.R;
            G = color.G;
            B = color.B;
        }

        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }

    class Program
    {
        static Pixel[,] LoadPixels(Bitmap bmp) // создание массива пикселей файла bmp
        {
            var pixels = new Pixel[bmp.Width, bmp.Height];
            for (var x = 0; x < bmp.Width; x++)
                for (var y = 0; y < bmp.Height; y++)
                    pixels[x, y] = new Pixel(bmp.GetPixel(x, y));
            return pixels;
        }

        static string GetImage(Pixel[,] pixels, string file) // создание файла bmp с новым словом
        {
            Console.WriteLine("Введите сообщение, которое вы хотите встроить");
            string word = Console.ReadLine();
            string text = GetWordsBytes(word);
            int n = pixels.GetLength(0);
            int m = pixels.GetLength(1);
            if ((m/8)* (n / 8) < text.Length/8) return null; // сообщение большое для данного файла

            var massiv = new int[8, 8];
            var coefs = new int[8, 8];
            int i, j, k, l;            
            for (i = 0; i + 8 <= n; i += 8)
                for (j = 0; j + 8 <= m; j += 8)
                {
                    for (k = 0; k < 8; k++)
                        for (l = 0; l < 8; l++)
                            massiv[k, l] = pixels[i + k, j + l].R;
                    coefs = TransformDCT(massiv);
                    if (text.Length!=0)
                    {
                        coefs = InsertText(coefs, text.Substring(0, 8));
                        text = text.Substring(8);
                    }
                    massiv = ReverseDCT(coefs);
                    for (k = 0; k < 8; k++)
                        for (l = 0; l < 8; l++)
                        {
                            if (massiv[k, l] < 0) massiv[k, l] = 0;
                            if (massiv[k, l] > 255) massiv[k, l] = 255;
                            pixels[i + k, j + l].R = (byte)massiv[k, l];
                        }
                }
            return CreateBitmap(pixels, file);
        }

        static string GetWordsBytes(string s) // получение строки последовательности битов сообщения
        {
            string text="";
            string bytes = "";
            byte[] code = Encoding.GetEncoding(1251).GetBytes(s); // последовательность байтов в ASCII
            foreach (var c in code)
            {
                string newByte = Convert.ToString(c, 2);
                for (int i = newByte.Length; i < 8; i++)
                    bytes += '0'; // добавление первых нулей
                bytes += newByte;
            }
            for (int i = 0; i < 8; i++)
                bytes += '0'; // добавление 8 нулей в конец
            for (int k = 0; k < bytes.Length; k++)
                for (int i = 0; i < 8; i++)
                    text += bytes[k];
            return text;
        }

        static int[,] TransformDCT (int[,] matrix) //получение коэффициентов ДКП
        {
            var dctM = new int[8, 8];
            int i, j, k, l;
            double ci, cj, dct, sum;
            for (i=0; i<8;i++)
                for (j=0;j<8;j++)
                {
                    ci = CountC(i);
                    cj = CountC(j);
                    sum = 0;
                    for (k = 0; k < 8; k++)
                        for (l = 0; l < 8; l++)
                        {
                            dct = matrix[k, l] * Math.Cos((2 * k + 1) * i * Math.PI / 16)
                                * Math.Cos((2 * l + 1) * j * Math.PI / 16);
                            sum += dct;
                        }
                    dctM[i, j] = (int)Math.Round(ci * cj * sum);
                }
            dctM[7, 7] += 1000;
            return dctM;
        }

        static double CountC (int i)
        {
            if (i == 0) return 1 / Math.Sqrt(8);
            return Math.Sqrt(0.25);
        }

        static int[,] InsertText(int[,] coefs, string text)//Вставляем сообщение
        {
            int i = 0;
            int j = 7;
            for(int k =0; k<8;k++)
            {
                if (text[k] =='0')
                {
                    if (Math.Abs(coefs[i, j]) % 2 == 1) coefs[i, j]--;
                }
                else
                {
                    if (Math.Abs(coefs[i, j]) % 2 == 0) coefs[i, j]++;
                }
                i++;
                j--;
            }
            return coefs;
        }

        static int[,] ReverseDCT(int[,] dctM)//обратное ДКП
        {
            var matrix = new int[8, 8];
            int i, j, k, l;
            double ci, cj, sum;
            for (k = 0; k < 8; k++)
                for (l = 0; l < 8; l++)                    
                {
                    sum = 0;
                    for (i = 0; i < 8; i++)
                        for (j = 0; j < 8; j++)
                        {
                            ci = CountC(i);
                            cj = CountC(j);
                            sum += dctM[i,j] * Math.Cos((2 * k + 1) * i * Math.PI / 16)
                                * Math.Cos((2 * l + 1) * j * Math.PI / 16) * ci * cj;                            
                        }
                    matrix[k,l] = (int)Math.Round(sum);
                }
            return matrix;
        }

        static string CreateBitmap(Pixel[,] newPixels, string file) // создание нового файла bmp
        {
            Random rnd = new Random();
            string name;
            name = rnd.Next(0, 999).ToString() + file; // создание рандомного имени
            Bitmap bmp = new Bitmap(file, true);
            for (int x = 0; x < newPixels.GetLength(0); x++)
                for (int y = 0; y < newPixels.GetLength(1); y++)
                    bmp.SetPixel(x, y, Color.FromArgb(newPixels[x, y].R, newPixels[x, y].G, newPixels[x, y].B));

            FileStream nameFile;
            nameFile = new FileStream(name, FileMode.Create); //открываем поток на запись результатов 
            bmp.Save(nameFile, System.Drawing.Imaging.ImageFormat.Bmp);
            nameFile.Close();
            return name;
        }

        static void CountPsnr(string name1, string name2) // подсчет psnr двух файлов
        {
            double psnr;
            double RMSE;
            Bitmap bmp1 = (Bitmap)Image.FromFile(name1);
            Bitmap bmp2 = (Bitmap)Image.FromFile(name2);
            var pixels1 = LoadPixels(bmp1);
            var pixels2 = LoadPixels(bmp2);
            float MSE = 0;
            for (int x = 0; x < pixels1.GetLength(0); x++)
                for (int y = 0; y < pixels1.GetLength(1); y++)
                {
                    MSE += (pixels1[x, y].R - pixels2[x, y].R) * (pixels1[x, y].R - pixels2[x, y].R);
                    MSE += (pixels1[x, y].G - pixels2[x, y].G) * (pixels1[x, y].G - pixels2[x, y].G);
                    MSE += (pixels1[x, y].B - pixels2[x, y].B) * (pixels1[x, y].B - pixels2[x, y].B);
                }
            if (MSE == 0) psnr= - 1;
            MSE /= (pixels1.GetLength(0) * pixels1.GetLength(1) * 3);
            RMSE = Math.Sqrt(MSE);
            float MAX = 255;
            psnr= 10 * Math.Log10(MAX * MAX / MSE);
            if (psnr == -1) Console.WriteLine("Сообщения одинаковые");
            else Console.WriteLine("PSNR: " + Convert.ToString(psnr));
            Console.WriteLine("RMSE: " + Convert.ToString(RMSE));

        }

        static string GetMessage(Pixel[,] pixels)//извлечение сообщения
        {
            int n = pixels.GetLength(0);
            int m = pixels.GetLength(1);
            var massiv = new int[8, 8];
            var coefs = new int[8, 8];
            int i, j, k, l;
            string text = "";
            int flag = 0;
            for (i = 0; i + 8 <= n; i += 8)
            {
                for (j = 0; j + 8 <= m; j += 8)
                {
                    for (k = 0; k < 8; k++)
                        for (l = 0; l < 8; l++)
                            massiv[k, l] = pixels[i + k, j + l].R;
                    coefs = TransformDCT(massiv);                  
                    text += GetByte(coefs);
                    if (text.Length >=8)
                        if (text.Substring(text.Length-8)=="00000000"&&text.Length%8==0)
                    {
                        flag = 1;
                        break;
                    }
                }
                if (flag == 1) break;
            }
            if (flag == 0) return "";
            return GetString(text);
        }

        static string GetByte(int[,] coefs)
        {
            int i = 0;
            int j = 7;
            int bit;
            int ones=0;
            int zeros=0;
            for (int k = 0; k < 8; k++)
            {                
                bit= Math.Abs(coefs[i, j]) % 2;
                if (bit == 0) zeros++;
                else ones++;
                i++;
                j--;
            }
            if (zeros>ones)return "0";
            return "1";
        }

        static string GetString (string byteString) // декодирование последовательности битов в сообщение
        {
            byte[] bytes = new byte[byteString.Length / 8];
            for (int i = 0; i + 8 <= byteString.Length; i += 8)            
                    bytes[i / 8] = Convert.ToByte(byteString.Substring(i, 8), 2);                 
            string s = Encoding.GetEncoding(1251).GetString(bytes);
            return s;
        }

        static void Main()
        {
            Console.WriteLine("Введите номер действия: 1. Встроить. 2. Извлечь.");
            int action = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Введите имя обрабатываемого файла");
            string picName = Console.ReadLine();


            Bitmap bmp; // обрабатываемый файл
            try
            {
                bmp = (Bitmap)Image.FromFile(picName);
            }
            catch
            {
                Console.WriteLine("Неверное имя файла");
                return;
            }

            var pixels = LoadPixels(bmp); // создаем массив пикселей обрабатываемого файла
            switch (action)
            {
                case 1:
                    var picNew = GetImage(pixels, picName); // создаем новый файл
                    if (picNew == null) Console.WriteLine("Слишком длинное сообщение");
                    else
                    {
                        Console.WriteLine("Имя нового файла: " + picNew);
                        CountPsnr(picName, picNew); // считаем psnr, rmse                        
                    }
                    break;
                case 2:
                    var message = GetMessage(pixels); // извлечение сообщения из файла                    
                    Console.WriteLine("Сообщение: " + message);
                    break;
                default:
                    Console.WriteLine("Неправильно выбран метод");
                    break;
            }
            Console.ReadKey();
        }
    }
}
