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

        // Cada chamada reserva um novo voluntário, mesmo após reiniciar o app.
        public static TrainingCsvWriter Create(string directory)
        {
            Directory.CreateDirectory(directory);
            // O contador também evita reutilizar IDs se uma pasta já tiver sido copiada/removida.
            // FileShare.None impede duas coletas simultâneas de reservarem o mesmo ID.
            using var counter = new FileStream(Path.Combine(directory, ".user_counter"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            int lastNumber;
            using (var reader = new StreamReader(counter, Encoding.UTF8, true, 1024, true))
            {
                string saved = reader.ReadToEnd();
                lastNumber = 0;
                if (saved.Length > 0 && (!int.TryParse(saved, NumberStyles.None,
                    CultureInfo.InvariantCulture, out lastNumber) || lastNumber < 0))
                    throw new IOException("Contador de voluntários inválido. Preserve as coletas e verifique .user_counter.");
            }
            foreach (string path in Directory.EnumerateFileSystemEntries(directory, "user_*"))
            {
                string number = Path.GetFileNameWithoutExtension(path).Substring("user_".Length);
                if (int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
                    lastNumber = Math.Max(lastNumber, value);
            }
            if (lastNumber == int.MaxValue)
                throw new IOException("Não há um número disponível para um novo voluntário.");
            int nextNumber = lastNumber + 1;
            byte[] savedNumber = Encoding.UTF8.GetBytes(nextNumber.ToString(CultureInfo.InvariantCulture));
            counter.Position = 0;
            counter.Write(savedNumber, 0, savedNumber.Length);
            counter.SetLength(savedNumber.Length);
            counter.Flush(true);

            string filePath = Path.Combine(directory, "user_" + nextNumber.ToString("D4", CultureInfo.InvariantCulture) + ".csv");
            // Mesmo em caso de conflito externo, uma coleta anterior nunca é sobrescrita.
            return new TrainingCsvWriter(filePath,
                new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read));
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
