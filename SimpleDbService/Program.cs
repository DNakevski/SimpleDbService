using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleDB;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SimpleDbService.Models;
using SimpleDbService.Service;
using SimpleDbService.Utils;

namespace SimpleDbService
{
    class Program
    {
        private static IConfigurationRoot _configuration;
        private static ISimpleDbService _sdbService;

        private static string _domainName;

        static async Task Main(string[] args)
        {
            SetupConfig();
            SetupSimpleDbService();

            await TestCreateItem("test-user-1");
            await TestBatchCreateItems(5, 2); //because user with id 1 is already created.
            await TestGetItem("test-user-1");
            await TestGetAllItems();
            await TestQueryItems();
            await TestDeleteItem("test-user-1");
            await TestBatchDeleteItems(new[] {"test-user-3", "test-user-4"});
            await TestUpdateItem("test-user-4");
            
            Console.ReadKey();
        }

        #region test methods
        static async Task TestCreateItem(string itemId)
        {
            var attributes = new List<SdbItemAttribute>
            {
                new SdbItemAttribute(StringConstants.USERNAME_ATTRIBUTE, "JohnD"),
                new SdbItemAttribute(StringConstants.FIRSTNAME_ATTRIBUTE, "John"),
                new SdbItemAttribute(StringConstants.LASTNAME_ATTRIBUTE, "Doe"),
                new SdbItemAttribute(StringConstants.AGE_ATTRIBUTE, "35")
            };

            var item = new SdbItem(itemId, attributes);

            try
            {
                var response = await _sdbService.CreateItemAsync(item);
                Console.WriteLine(response != HttpStatusCode.OK
                    ? $"HttpResponse from the create ite action return code: {response}."
                    : "New user was created.");
            }
            catch (AmazonSimpleDBException)
            {
                Console.WriteLine("There was and error while creating the item");
            }
        }

        static async Task TestBatchCreateItems(int numberOfItems, int startId)
        {
            var testUsers = new List<SdbItem>();
            for (var i = startId; i <= (startId + numberOfItems); i++)
            {
                var itemId = "test-item-" + i;
                var attributes = new List<SdbItemAttribute>
                {
                    new SdbItemAttribute(StringConstants.USERNAME_ATTRIBUTE, "test-username-" + i),
                    new SdbItemAttribute(StringConstants.FIRSTNAME_ATTRIBUTE, "test-name-" + i),
                    new SdbItemAttribute(StringConstants.LASTNAME_ATTRIBUTE, "test-lastname-" + i),
                    new SdbItemAttribute(StringConstants.AGE_ATTRIBUTE, (20 + i).ToString())
                };

                testUsers.Add(new SdbItem(itemId, attributes));
            }

            try
            {
                await _sdbService.BatchCreateItemsAsync(testUsers);
                Console.WriteLine(testUsers.Count + " users were created with the batch create action");
            }
            catch (AmazonSimpleDBException)
            {
                Console.WriteLine("There was and error while batch creating the items");
            }
        }


        static async Task TestGetItem(string itemId)
        {
            try
            {
                var item = await _sdbService.GetItemAsync(itemId);
                if (item != null)
                {
                    Console.WriteLine("Content of the returned item: " + JsonConvert.SerializeObject(item));
                }
                else
                {
                    Console.WriteLine($"ERROR: Item with the specified id {itemId} was not found.");
                }
                    
            }
            catch (AmazonSimpleDBException)
            {
                Console.WriteLine($"There was and error while getting the item with id: {itemId}");
            }

            
        }

        static async Task TestGetAllItems()
        {
            try
            {
                var items = await _sdbService.GetAllItems();
                if (items.Any())
                {
                    Console.WriteLine("Content of the returned items: " + JsonConvert.SerializeObject(items));
                }
                else
                {
                    Console.WriteLine("INFO: There are no items in the SimpleDb domain");
                }

            }
            catch (AmazonSimpleDBException)
            {
                Console.WriteLine("There was and error while getting the items");
            }
        }

        static async Task TestQueryItems()
        {
            try
            {
                //select all users that have age bigger than 30
                var sampleQuery = $"select * from `{_domainName}` where Age > 30";
                var items = await _sdbService.QueryItemsAsync(sampleQuery);
                if (items.Any())
                {
                    Console.WriteLine("Content of the returned items: " + JsonConvert.SerializeObject(items));
                }
                else
                {
                    Console.WriteLine("INFO: Items for the specified query were not found.");
                }

            }
            catch (AmazonSimpleDBException)
            {
                Console.WriteLine("There was and error while getting the items");
            }
        }

        static async Task TestDeleteItem(string itemId)
        {
            try
            {
                var response = await _sdbService.DeleteItemAsync(itemId);
                if (response.HttpStatusCode == HttpStatusCode.OK)
                    Console.WriteLine("The item has been successfully deleted.");
                else
                    Console.WriteLine("The response returned status code: " + response.HttpStatusCode);
            }
            catch (AmazonSimpleDBException)
            {
                Console.WriteLine("There was and error while deleting the item with id " + itemId);
            }
        }

        static async Task TestBatchDeleteItems(string[] ids)
        {
            try
            {
                await _sdbService.BatchDeleteItemsAsync(ids);
                Console.WriteLine(ids.Length + " items were deleted from the SimpleDb domain");
            }
            catch (AmazonSimpleDBException)
            {
                Console.WriteLine("There was and error while batch deleting the items");
            }
        }

        static async Task TestUpdateItem(string id)
        {
            var attributes = new List<SdbItemAttribute>
            {
                new SdbItemAttribute(StringConstants.USERNAME_ATTRIBUTE, "test-username-" + "-UPDATED"),
                new SdbItemAttribute(StringConstants.FIRSTNAME_ATTRIBUTE, "test-name-" + "-UPDATED"),
                new SdbItemAttribute(StringConstants.LASTNAME_ATTRIBUTE, "test-lastname-" + "-UPDATED")
            };

            try
            {
                await _sdbService.UpdateItemAsync(id, attributes);
                Console.WriteLine("The item has been updated");
            }
            catch (AmazonSimpleDBException)
            {
                Console.WriteLine("There was and error while batch updating the item");
            }

        }

        #endregion

        #region configuration section

        /// <summary>
        /// Setup basic configuration of the app
        /// </summary>
        static void SetupConfig()
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true);

            _configuration = builder.Build();
        }

        /// <summary>
        /// Setup the SimpleDb service
        /// This setup is going to work if the resource using this peace of code is already authorized to use the SimpleDb service.
        /// Example for this can be if the code is executed from an AWS Lambda function which already has permissions to use the service.
        /// In that case, credentials are not required.
        /// </summary>
        static void SetupSimpleDbService()
        {
            _domainName = _configuration.GetSection("DomainConfiguration:Name").Value;
            var region = _configuration.GetSection("DomainConfiguration:Region").Value;
            Console.WriteLine($"Configuring SimpleDb service for region: {region} and domain: {_domainName}...");
            _sdbService = new Service.SimpleDbService(_domainName, region);
        }

        /// <summary>
        /// Setup the SimpleDb service with credentials provided from the settings file.
        /// This configuration can work even from anywhere. If valid credentials are provided from the appSettings.json file,
        /// the service request are going to be authenticated and therefore can perform all the authorized actions on the service.
        /// </summary>
        static void SetupSimpleDbServiceWithAuth()
        {
            _domainName = _configuration.GetSection("DomainConfiguration:Name").Value;
            var region = RegionEndpoint.GetBySystemName(_configuration.GetSection("DomainConfiguration:Region").Value);
            var accessKey = _configuration.GetSection("DomainConfiguration:AccessKey").Value;
            var secretKey = _configuration.GetSection("DomainConfiguration:SecretKey").Value;
            var credentials = new BasicAWSCredentials(accessKey, secretKey);

            Console.WriteLine($"Configuring SimpleDb service for region: {region.DisplayName} and domain: {_domainName}...");
            _sdbService = new Service.SimpleDbService(credentials, _domainName, region);
        }

        #endregion
    }
}
