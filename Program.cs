using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using NAudio.Wave;

namespace ADOSH_LR1
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string filePath = "sukunas-ryoiki-tenkai.wav";
            int smoothingInterval = 5; // Интервал сглаживания в семплах
            int scanWindowStart = 200; // Начальная точка окна сканирования
            int scanWindowEnd = 700; // Конечная точка окна сканирования

            // 1. Загрузка аудиофайла
            var audioData = LoadAudioFile(filePath);

            // 2. Сглаживание сигнала
            var smoothedSignal = SmoothSignal(audioData, smoothingInterval);

            // 3. Анализ сигналов для выделения точек 0-1-2
            var features = ExtractFeatures(smoothedSignal, scanWindowStart, scanWindowEnd);

            // 4. Построение треугольника
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TriangleForm(features));
        }

        static float[] LoadAudioFile(string filePath)
        {
            using (var reader = new WaveFileReader(filePath))
            {
                var samples = new float[reader.SampleCount];
                int index = 0;
                while (reader.Position < reader.Length)
                {
                    samples[index++] = reader.ReadNextSampleFrame()[0]; // Mono
                }
                Console.WriteLine($"Аудиофайл загружен: {samples.Length} семплов.");
                return samples;
            }
        }

        static float[] SmoothSignal(float[] signal, int interval)
        {
            var smoothedSignal = new float[signal.Length];
            for (int i = 0; i < signal.Length; i++)
            {
                int start = Math.Max(0, i - interval / 2);
                int end = Math.Min(signal.Length - 1, i + interval / 2);
                smoothedSignal[i] = signal.Skip(start).Take(end - start + 1).Average();
            }
            Console.WriteLine("Сглаживание завершено.");
            return smoothedSignal;
        }

        static (float v1, float v2)[] ExtractFeatures(float[] signal, int start, int end)
        {
            var features = new (float v1, float v2)[(end - start) / 3];
            for (int i = start, index = 0; i < end - 2; i += 3, index++)
            {
                float v1 = Math.Abs(signal[i + 1] - signal[i]);
                float v2 = Math.Abs(signal[i + 2] - signal[i + 1]);
                features[index] = (v1, v2);
            }
            Console.WriteLine("Извлечение признаков завершено.");
            return features;
        }
    }

    // Класс для визуализации
    public class TriangleForm : Form
    {
        private (float v1, float v2)[] features;

        public TriangleForm((float v1, float v2)[] features)
        {
            this.features = features;
            this.Text = "Треугольник голосных";
            this.Size = new Size(800, 800);
            this.Paint += new PaintEventHandler(DrawTriangle);
        }

        private void DrawTriangle(object sender, PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.Clear(Color.White);

            // Определяем масштабы
            float scaleX = 700; // Коэффициент масштабирования по X
            float scaleY = 700; // Коэффициент масштабирования по Y

            // Центр треугольника
            int centerX = this.ClientSize.Width / 2;
            int centerY = this.ClientSize.Height / 2;

            // Рисуем точки
            foreach (var (v1, v2) in features)
            {
                int x = centerX + (int)(v1 * scaleX);
                int y = centerY - (int)(v2 * scaleY); // Уменьшаем y для инверсии по оси Y
                graphics.FillEllipse(Brushes.Blue, x - 3, y - 3, 6, 6);
            }

            // Подписи осей
            graphics.DrawString("V1", new Font("Arial", 12), Brushes.Black, centerX + scaleX + 10, centerY);
            graphics.DrawString("V2", new Font("Arial", 12), Brushes.Black, centerX, centerY - scaleY - 20);
            graphics.DrawLine(Pens.Black, centerX - 10, centerY, centerX + scaleX + 10, centerY); // Ось X
            graphics.DrawLine(Pens.Black, centerX, centerY + 10, centerX, centerY - scaleY - 10); // Ось Y
        }
    }
}
