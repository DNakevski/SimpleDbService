using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.SimpleDB.Model;
using SimpleDbService.Models;

namespace SimpleDbService.Service
{
    public interface ISimpleDbService
    {
        /// <summary>
        /// Returns single item based on the id
        /// </summary>
        Task<SdbItem> GetItemAsync(string itemId, bool consistentRead = false);

        /// <summary>
        /// Returns all items
        /// </summary>
        Task<IEnumerable<SdbItem>> GetAllItems();

        /// <summary>
        /// Query items based on query expression
        /// </summary>
        Task<IEnumerable<SdbItem>> QueryItemsAsync(string query, bool consistentRead = false);

        /// <summary>
        /// Creates single item
        /// </summary>
        Task<HttpStatusCode> CreateItemAsync(SdbItem item);

        /// <summary>
        /// Creates multiple items simultaneously
        /// </summary>
        Task BatchCreateItemsAsync(IEnumerable<SdbItem> items);

        /// <summary>
        /// Deletes single item based on the id
        /// </summary>
        Task<DeleteAttributesResponse> DeleteItemAsync(string itemId);

        /// <summary>
        /// Deletes multiple items based on their ids
        /// </summary>
        Task BatchDeleteItemsAsync(string[] ids);

        /// <summary>
        /// Updates single item
        /// </summary>
        Task UpdateItemAsync(string itemId, IEnumerable<SdbItemAttribute> newValues, bool removeUnspecifiedAttributes = false);

        /// <summary>
        /// Updates single item
        /// </summary>
        Task UpdateItemAsync(string itemId, Dictionary<string, string> newValues, bool removeUnspecifiedAttributes = false);
    }
}
