using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using SimpleDbService.Models;
using SimpleDbService.Utils;
using Attribute = Amazon.SimpleDB.Model.Attribute;

namespace SimpleDbService.Service
{
    public class SimpleDbService : ISimpleDbService
    {
        private readonly AmazonSimpleDBClient _client;
        private readonly string _simpleDbDomain;
        private const string ExistsAttributeName = "Exists";

        public SimpleDbService()
        {
            _client = new AmazonSimpleDBClient();
        }

        public SimpleDbService(string simpleDbDomain, string region) : this(simpleDbDomain, RegionEndpoint.GetBySystemName(region))
        {
        }

        public SimpleDbService(string simpleDbDomain, RegionEndpoint region)
        {
            _simpleDbDomain = simpleDbDomain;
            _client = new AmazonSimpleDBClient(region);
        }

        public SimpleDbService(AWSCredentials credentials, string simpleDbDomain, RegionEndpoint region)
        {
            _simpleDbDomain = simpleDbDomain;
            _client = new AmazonSimpleDBClient(credentials, region);
        }

        /// <summary>
        /// Returns single item based on the id
        /// </summary>
        public async Task<SdbItem> GetItemAsync(string itemId, bool consistentRead = false)
        {
            try
            {
                var attributes = await RetrieveItemAttributesFromDb(itemId, consistentRead);

                if (!attributes.Any())
                    return null;

                var item = new SdbItem(itemId, SimpleDbHelper.ConvertAttributesToItemAttributes(attributes));
                return item;
            }
            catch (AmazonSimpleDBException e)
            {
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Returns all items
        /// </summary>
        public async Task<IEnumerable<SdbItem>> GetAllItems()
        {
            try
            {
                var selectRequest = new SelectRequest()
                {
                    SelectExpression = "select * from `" + _simpleDbDomain + "`"
                };

                var response = await _client.SelectAsync(selectRequest);
                if (response.HttpStatusCode != HttpStatusCode.OK)
                    return new List<SdbItem>();

                return !response.Items.Any() ? new List<SdbItem>() : SimpleDbHelper.ConvertSimpleDbItemsToMetaStoreItems(response.Items);
            }
            catch (AmazonSimpleDBException e)
            {
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Query items based on query expression. Consistent read is by default set to false to ensure performance
        /// </summary>
        public async Task<IEnumerable<SdbItem>> QueryItemsAsync(string query, bool consistentRead = false)
        {
            try
            {
                var selectRequest = new SelectRequest()
                {
                    SelectExpression = query,
                    ConsistentRead = consistentRead
                };

                var response = await _client.SelectAsync(selectRequest);
                if (response.HttpStatusCode != HttpStatusCode.OK)
                    return new List<SdbItem>();

                return !response.Items.Any() ? new List<SdbItem>() : SimpleDbHelper.ConvertSimpleDbItemsToMetaStoreItems(response.Items);
            }
            catch (AmazonSimpleDBException e)
            {
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Creates single item
        /// </summary>
        public async Task<HttpStatusCode> CreateItemAsync(SdbItem item)
        {
            try
            {
                //Add additional attribute for the Exists flag on the newly created item
                item.Attributes = item.Attributes.Concat(new[] { new SdbItemAttribute(ExistsAttributeName, "1"),  });

                var convertedAttributes = SimpleDbHelper.ConvertItemAttributesToReplaceableAttributes(item.Attributes);
                var putRequest = new PutAttributesRequest(_simpleDbDomain, item.ItemName, convertedAttributes.ToList());
                var putResponse = await _client.PutAttributesAsync(putRequest);
                return putResponse.HttpStatusCode;
            }
            catch (AmazonSimpleDBException e)
            {
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Creates multiple items simultaneously
        /// </summary>
        public async Task BatchCreateItemsAsync(IEnumerable<SdbItem> items)
        {
            var replaceableItems = new List<ReplaceableItem>();

            foreach (var item in items)
            {
                //Add additional attribute for the Exists flag on the newly created item
                item.Attributes = item.Attributes.Concat(new[] { new SdbItemAttribute(ExistsAttributeName, "1"),  });
                var convertedAttributes = SimpleDbHelper.ConvertItemAttributesToReplaceableAttributes(item.Attributes);
                replaceableItems.Add(new ReplaceableItem(item.ItemName, convertedAttributes.ToList()));
            }

            try
            {
                var batchPutRequest = new BatchPutAttributesRequest(_simpleDbDomain, replaceableItems);
                await _client.BatchPutAttributesAsync(batchPutRequest);
            }
            catch (AmazonSimpleDBException e)
            {
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Deletes single item based on the id
        /// </summary>
        public async Task<DeleteAttributesResponse> DeleteItemAsync(string itemId)
        {
            try
            {
                var deleteAttrRequest = new DeleteAttributesRequest(_simpleDbDomain, itemId);
                return await _client.DeleteAttributesAsync(deleteAttrRequest);
            }
            catch (AmazonSimpleDBException e)
            {
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Deletes multiple items based on their ids
        /// </summary>
        public async Task BatchDeleteItemsAsync(string[] ids)
        {
            try
            {
                //if the list attributes is set to null it is going to delete all the attributes(the entire item),
                //otherwise it is going to delete only the specified attributes in the items and not the entire item
                var deletableItems = ids.Select(id => new DeletableItem(id, null)).ToList();
                var request = new BatchDeleteAttributesRequest(_simpleDbDomain, deletableItems);
                await _client.BatchDeleteAttributesAsync(request);
            }
            catch (AmazonSimpleDBException e)
            {
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Updates single item
        /// </summary>
        public async Task UpdateItemAsync(string itemId, IEnumerable<SdbItemAttribute> attributes, bool removeUnspecifiedAttributes = false)
        {
            var newValues = attributes.ToDictionary(x => x.Name, y => y.Value);
            await UpdateItemAsync(itemId, newValues, removeUnspecifiedAttributes);
        }

        /// <summary>
        /// Updates single item
        /// </summary>
        public async Task UpdateItemAsync(string itemId, Dictionary<string, string> newValues, bool removeUnspecifiedAttributes = false)
        {
            var itemAttributes = await RetrieveItemAttributesFromDb(itemId, true);
            if (!itemAttributes.Any())
            {
                return;
            }

            var existingAttributes = itemAttributes.ToDictionary(attr => attr.Name, attr => attr);
            var attributesToUpdate = new List<ReplaceableAttribute>();
            var attributesToDelete = new List<Attribute>();

            foreach (var (key, value) in newValues)
            {
                if (value == "") //the attribute needs to be deleted
                {
                    if (existingAttributes.ContainsKey(key)) //otherwise it is already not present
                        attributesToDelete.Add(existingAttributes[key]);
                    continue;
                }

                //if it is already present replace it's value, otherwise add it
                var replace = existingAttributes.ContainsKey(key);
                attributesToUpdate.Add(new ReplaceableAttribute(key, value, replace));
            }
            if (removeUnspecifiedAttributes)
                foreach (var (key, value) in existingAttributes)
                {
                    if (key != ExistsAttributeName && !newValues.ContainsKey(key))
                        attributesToDelete.Add(value);
                }

            try
            {
                //update the attributes
                var putRequest = new PutAttributesRequest(_simpleDbDomain, itemId, attributesToUpdate,
                    new UpdateCondition(ExistsAttributeName, "1", true));
                await _client.PutAttributesAsync(putRequest);

                //delete the attributes that need to be deleted
                if (attributesToDelete.Any())
                {
                    var delRequest = new DeleteAttributesRequest(_simpleDbDomain, itemId, attributesToDelete);
                    await _client.DeleteAttributesAsync(delRequest);
                }
            }
            catch (AmazonSimpleDBException e)
            {
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        private async Task<List<Attribute>> RetrieveItemAttributesFromDb(string itemId, bool consistentRead = false)
        {
            var request = new GetAttributesRequest(_simpleDbDomain, itemId) { ConsistentRead = consistentRead };
            var response = await _client.GetAttributesAsync(request);
            return response.Attributes;
        }
    }
}
