using System;
using System.Globalization;
using System.IO;
using System.Text;
using MyoGuess.Gameplay;

namespace MyoGuess.Training
{
    // Independente da cena: novos canais de sinais podem ser acrescentados aqui no futuro.
    public sealed class TrainingCsvWriter : IDisposable
    {
        public const string Header = "timestamp_utc,tempo_coleta_s,ciclo,etapa,fase,gesto_esperado,gesto_reconhecido";
        private readonly StreamWriter writer;

        public string FilePath { get; }
        public int SampleCount { get; private set; }

        private TrainingCsvWriter(string filePath, FileStream stream)
        {
            FilePath = filePath;
            writer = new StreamWriter(stream, new UTF8Encoding(false));
            try
            {
                writer.WriteLine(Header);
                writer.Flush();
            }
            catch
            {
                writer.Dispose();
                throw;
            }
        }

        public static TrainingCsvWriter Create(string directory)
        {
            Directory.CreateDirectory(directory);
            int lastNumber = 0;
            foreach (string path in Directory.EnumerateFiles(directory, "COLETA_*.csv"))
            {
                string number = Path.GetFileNameWithoutExtension(path).Substring("COLETA_".Length);
                if (int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
                    lastNumber = Math.Max(lastNumber, value);
            }

            while (lastNumber < int.MaxValue)
            {
                lastNumber++;
                string path = Path.Combine(directory, "COLETA_" + lastNumber.ToString(CultureInfo.InvariantCulture) + ".csv");
                FileStream stream;
                try
                {
                    // CreateNew protege coletas anteriores, inclusive se outro gravador abrir ao mesmo tempo.
                    stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                }
                catch (IOException) when (File.Exists(path))
                {
                    continue;
                }
                return new TrainingCsvWriter(path, stream);
            }
            throw new IOException("Não há um número disponível para uma nova coleta.");
        }

        public void WriteSample(DateTimeOffset timestamp, double elapsedSeconds, int cycle, int step,
            TrainingInterfaceController.TrainingPhase phase, GestureType expected, GestureType recognized)
        {
            // Formato e separador decimal fixos, independentemente do idioma do Quest/PC.
            writer.WriteLine(string.Join(",",
                timestamp.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
                elapsedSeconds.ToString("F6", CultureInfo.InvariantCulture),
                cycle.ToString(CultureInfo.InvariantCulture),
                step.ToString(CultureInfo.InvariantCulture),
                phase.ToString(), expected.ToString(), recognized.ToString()));
            SampleCount++;
        }

        public void Flush() => writer.Flush();
        public void Dispose() => writer.Dispose();
    }
}
