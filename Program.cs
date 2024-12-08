using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using NAudio.Wave;
using System.IO;

namespace ADOSH_LR1
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string filePath = "test1.wav"; // Шлях до аудіофайлу
            int smoothingInterval = 5;                    // Інтервал згладжування в семплах
            int scanWindowStart = 200;                    // Початок вікна сканування
            int scanWindowEnd = 700;                      // Кінець вікна сканування

            try
            {
                // 1. Загрузка аудіофайлу
                var audioData = LoadAudioFile(filePath);

                // 2. Згладжування сигналу
                var smoothedSignal = SmoothSignal(audioData, smoothingInterval);

                // 3. Вибір точок і обчислення ознак
                var features = ExtractFeatures(smoothedSignal, scanWindowStart, scanWindowEnd);

                // 4. Побудова трикутника голосних
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TriangleForm(features));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка: {ex.Message}");
            }
        }

        // Метод для завантаження аудіофайлу
        static float[] LoadAudioFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException($"Файл {filePath} не знайдено.");
            }

            using (var reader = new WaveFileReader(filePath))
            {
                var samples = new List<float>();
                while (reader.Position < reader.Length)
                {
                    var frame = reader.ReadNextSampleFrame();
                    if (frame != null && frame.Length > 0)
                    {
                        samples.Add(frame[0]); // Беремо тільки перший канал (Mono)
                    }
                }

                if (samples.Count == 0)
                {
                    throw new InvalidOperationException("Файл не містить семплів.");
                }

                Console.WriteLine($"Аудіофайл завантажено: {samples.Count} семплів.");
                return samples.ToArray();
            }
        }


        // Метод для згладжування сигналу
        static float[] SmoothSignal(float[] signal, int interval)
        {
            var smoothedSignal = new float[signal.Length];
            for (int i = 0; i < signal.Length; i++)
            {
                int start = Math.Max(0, i - interval / 2);
                int end = Math.Min(signal.Length - 1, i + interval / 2);

                // Перевірка, чи є дані в межах вікна
                if (start >= end)
                {
                    smoothedSignal[i] = signal[i]; // Якщо вікно занадто маленьке, залишаємо оригінальне значення
                }
                else
                {
                    smoothedSignal[i] = signal.Skip(start).Take(end - start + 1).Average();
                }
            }
            Console.WriteLine("Сгладжування завершено.");
            return smoothedSignal;
        }

        // Метод для вибору точок і обчислення ознак
        static (float v1, float v2)[] ExtractFeatures(float[] signal, int start, int end)
        {
            var features = new List<(float v1, float v2)>();
            for (int i = start; i <= end - 3; i += 3)
            {
                float v1 = Math.Abs(signal[i + 1] - signal[i]);        // Різниця між 0 і 1
                float v2 = Math.Abs(signal[i + 2] - signal[i + 1]);    // Різниця між 1 і 2
                features.Add((v1, v2));
            }

            Console.WriteLine("Ознаки до нормалізації:");
            foreach (var (v1, v2) in features.Take(10)) // Виводимо тільки перші 10 для зручності
            {
                Console.WriteLine($"v1: {v1}, v2: {v2}");
            }

            // Нормалізуємо значення для коректного масштабування
            float maxV1 = features.Max(f => f.v1);
            float maxV2 = features.Max(f => f.v2);

            // Уникаємо ділення на нуль
            if (maxV1 == 0) maxV1 = 1;
            if (maxV2 == 0) maxV2 = 1;

            for (int i = 0; i < features.Count; i++)
            {
                features[i] = (features[i].v1 / maxV1, features[i].v2 / maxV2);
            }

            Console.WriteLine("Ознаки після нормалізації:");
            foreach (var (v1, v2) in features.Take(10)) // Виводимо тільки перші 10 для зручності
            {
                Console.WriteLine($"v1: {v1}, v2: {v2}");
            }

            if (!features.Any())
            {
                Console.WriteLine("Попередження: не виділено жодної ознаки.");
            }

            return features.ToArray();
        }


        // Клас для відображення трикутника голосних
        public class TriangleForm : Form
        {
            private (float v1, float v2)[] features;

            public TriangleForm((float v1, float v2)[] features)
            {
                this.features = features;
                this.Text = "Трикутник голосних";
                this.Size = new Size(800, 800);
                this.Paint += new PaintEventHandler(DrawTriangle);
            }

            private void DrawTriangle(object sender, PaintEventArgs e)
            {
                var graphics = e.Graphics;
                graphics.Clear(Color.White);

                // Масштабування для відображення
                float scaleX = 300; // Масштаб по X
                float scaleY = 300; // Масштаб по Y

                // Центр для координат
                int centerX = this.ClientSize.Width / 2;
                int centerY = this.ClientSize.Height / 2;

                // Перевірка: чи є точки для відображення
                if (features.Length == 0)
                {
                    graphics.DrawString("Немає даних для відображення", new Font("Arial", 16), Brushes.Red, centerX - 150, centerY);
                    return;
                }

                // Малюємо точки
                foreach (var (v1, v2) in features)
                {
                    int x = centerX + (int)(v1 * scaleX);
                    int y = centerY - (int)(v2 * scaleY); // Інвертуємо Y
                    graphics.FillEllipse(Brushes.Blue, x - 3, y - 3, 6, 6);
                }

                // Малюємо осі
                graphics.DrawLine(Pens.Black, centerX - 10, centerY, centerX + scaleX + 10, centerY); // X
                graphics.DrawLine(Pens.Black, centerX, centerY + 10, centerX, centerY - scaleY - 10); // Y
                graphics.DrawString("V1", new Font("Arial", 12), Brushes.Black, centerX + scaleX + 10, centerY);
                graphics.DrawString("V2", new Font("Arial", 12), Brushes.Black, centerX, centerY - scaleY - 20);
            }
        }
    }
}