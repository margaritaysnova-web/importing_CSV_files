using System.Text;

namespace Pr02Ber.Services
{
    public static class CsvLineParser
    {
        public static IReadOnlyList<string> Parse(string line)
        {
            var result = new List<string>();
            if (line == null)
                return result;

            var currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    // Если мы внутри кавычек
                    if (c == '"')
                    {
                        // Проверяем экранированную кавычку ("")
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            currentField.Append('"');
                            i++; // Пропускаем следующую кавычку
                        }
                        else
                        {
                            // Закрывающая кавычка
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        // Обычный символ внутри кавычек (включая запятые)
                        currentField.Append(c);
                    }
                }
                else
                {
                    // Если мы снаружи кавычек
                    if (c == '"')
                    {
                        // Открывающая кавычка
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        // Конец поля. Добавляем в список и обрезаем пробелы
                        result.Add(currentField.ToString().Trim());
                        currentField.Clear();
                    }
                    else
                    {
                        // Обычный символ
                        currentField.Append(c);
                    }
                }
            }

            // Добавляем последнее поле после цикла
            result.Add(currentField.ToString().Trim());

            return result;
        }
    }
}