using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pr02Ber.Services;

namespace Pr02Ber.Tests
{
    public class CsvLineParserTests
    {
        // Тест 1: Простая строка без кавычек
        [Fact]
        public void Parse_SimpleLine_ReturnsCorrectFields()
        {
            string line = "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad 3-15";

            var result = CsvLineParser.Parse(line);

            Assert.Equal(12, result.Count);
            Assert.Equal("1001", result[0]);
            Assert.Equal("Ноутбук Lenovo IdeaPad 3", result[1]);
            Assert.Equal("IdeaPad 3-15", result[11]);
        }

        // Тест 2: Пустые поля
        [Fact]
        public void Parse_EmptyFields_ReturnsEmptyStrings()
        {
            string line = "1011,Блок питания Corsair CV550,,4990,RUB,Компьютеры,9,,true,Corsair,60,CV550";

            var result = CsvLineParser.Parse(line);

            Assert.Equal(12, result.Count);
            Assert.Equal(string.Empty, result[2]); // Третье поле пустое
            Assert.Equal(string.Empty, result[7]); // Восьмое поле пустое
        }

        // Тест 3: Поле с запятой внутри кавычек
        [Fact]
        public void Parse_CommaInsideQuotes_ReturnsSingleField()
        {
            string line = "1012,\"Ноутбук, игровой\",Тестовая строка,59990,RUB,Компьютеры,1,/images/notebook.jpg,true,ASUS,36,VivoBook";

            var result = CsvLineParser.Parse(line);

            Assert.Equal(12, result.Count);
            Assert.Equal("Ноутбук, игровой", result[1]); // Запятая не разделила поле
        }

        // Тест 4: Экранированные кавычки
        [Fact]
        public void Parse_EscapedQuotes_ReturnsSingleQuote()
        {
            string line = "1013,\"Название \"\"Special\"\"\",Описание,100,RUB,Категория,5,/img.jpg,true,Brand,12,Model";

            var result = CsvLineParser.Parse(line);

            Assert.Equal(12, result.Count);
            Assert.Equal("Название \"Special\"", result[1]); // Двойные кавычки превратились в одинарные
        }
    }
}