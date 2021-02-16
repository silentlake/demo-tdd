using System;
using Xunit;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CloudAcademy.Bitcoin.Tests
{
    public class BitcoinConverterSvcShould
    {
        private const string MOCK_RESPONSE_JSON = @"{""time"": {""updated"": ""Oct 15, 2020 22:55:00 UTC"",""updatedISO"": ""2020-10-15T22:55:00+00:00"",""updateduk"": ""Oct 15, 2020 at 23:55 BST""},""chartName"": ""Bitcoin"",""bpi"": {""USD"": {""code"": ""USD"",""symbol"": ""&#36;"",""rate"": ""11,486.5341"",""description"": ""United States Dollar"",""rate_float"": 11486.5341},""GBP"": {""code"": ""GBP"",""symbol"": ""&pound;"",""rate"": ""8,900.8693"",""description"": ""British Pound Sterling"",""rate_float"": 8900.8693},""EUR"": {""code"": ""EUR"",""symbol"": ""&euro;"",""rate"": ""9,809.3278"",""description"": ""Euro"",""rate_float"": 9809.3278}}}";

        private ConverterSvc mockConverter;

        public BitcoinConverterSvcShould()
        {
            //arrange
            mockConverter = GetMockBitcoinConverterService();
        }

        private ConverterSvc GetMockBitcoinConverterService() {
            var handlerMock = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(MOCK_RESPONSE_JSON),
            };

            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(response);

            var httpClient = new HttpClient(handlerMock.Object);

            var converter = new ConverterSvc(httpClient);

            return converter;
        }

        [Theory]
        [InlineData(ConverterSvc.Currency.USD,11486.5341)]
        [InlineData(ConverterSvc.Currency.GBP,8900.8693)]
        [InlineData(ConverterSvc.Currency.EUR,9809.3278)]
        public async void GetExchangeRate_Currency_ReturnsCurrencyExchangeRate(ConverterSvc.Currency currency, double expected)
        {
            //act
            var exchageRate = await mockConverter.GetExchangeRate(currency);

            //assert
            Assert.Equal(expected, exchageRate);
        }

        [Theory]
        [InlineData(ConverterSvc.Currency.USD,1,11486.5341)]
        [InlineData(ConverterSvc.Currency.USD,2,2*11486.5341)]
        [InlineData(ConverterSvc.Currency.GBP,1,8900.8693)]
        [InlineData(ConverterSvc.Currency.GBP,2,2*8900.8693)]
        [InlineData(ConverterSvc.Currency.EUR,1,9809.3278)]
        [InlineData(ConverterSvc.Currency.EUR,2,2*9809.3278)]
        public async void ConvertBitcoins_BitcoinsToCurrency_ReturnsCurrency(ConverterSvc.Currency currency, int bitcoins, double expected)
        {
            //act
            var dollars = await mockConverter.ConvertBitcoins(currency, bitcoins);

            //assert
            Assert.Equal(expected, dollars);
        }

        [Fact]
        public async void ConvertBitcoins_BitcoinsAPIServiceUnavailible_ReturnsNegativeOne()
        {
        //Given
        var handlerMock = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.ServiceUnavailable,
            Content = new StringContent("not working")
        };

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
                )
            .ReturnsAsync(response);
        
        var httpClient = new HttpClient(handlerMock.Object);

        var converter = new ConverterSvc(httpClient);
        //When
        var amount = await converter.ConvertBitcoins(ConverterSvc.Currency.USD, 5);
        
        //Then
        var expected = -1;
        AssemblyLoadEventArgs.Equals(expected, amount);
        }

        [Fact]
        public async void ConvertBitcoins_BitcoinsLessThanZero_ThrowsArgumentException()
        {
        //When
        Task result() => mockConverter.ConvertBitcoins(ConverterSvc.Currency.USD, -1);
        //Then
        await Assert.ThrowsAsync<ArgumentException>(result);
        }
    }
}
