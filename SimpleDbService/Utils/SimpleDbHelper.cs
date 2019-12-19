using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.SimpleDB.Model;
using SimpleDbService.Models;
using Attribute = Amazon.SimpleDB.Model.Attribute;
using Item = Amazon.SimpleDB.Model.Item;

namespace SimpleDbService.Utils
{
    public class SimpleDbHelper
    {
        /// <summary>
        /// Converts SimpleDb Attributes to ItemAttributes.
        /// </summary>
        public static IEnumerable<SdbItemAttribute> ConvertAttributesToItemAttributes(IEnumerable<Attribute> attributes)
        {
            return attributes.Select(x => new SdbItemAttribute(x.Name, x.Value)).ToList();
        }

        /// <summary>
        /// Converts ItemAttributes to SimpleDb ReplaceableAttributes.
        /// </summary>
        public static IEnumerable<ReplaceableAttribute> ConvertItemAttributesToReplaceableAttributes(
            IEnumerable<SdbItemAttribute> attributes)
        {
            return attributes.Select(x => new ReplaceableAttribute(x.Name, x.Value, x.Replace)).ToList();
        }

        /// <summary>
        /// Converts SimpleDb items to MetaStore items.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static IEnumerable<SdbItem> ConvertSimpleDbItemsToMetaStoreItems(List<Item> items)
        {
            return items.Select(x => new SdbItem(x.Name, ConvertAttributesToItemAttributes(x.Attributes)));
        }

        /// <summary>
        /// Generates GUID id for item
        /// </summary>
        public static string GenerateItemId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}

