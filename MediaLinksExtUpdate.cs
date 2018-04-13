using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaLinks
{
    public class MediaLinksExtUpdate
    {
        /// <summary>
        /// Replacing (.ashx) extension for media library items inside RTE with the original extensions
        /// </summary>
        public void ChangeMediaExtenstions()
        {

            // Get the master database
            Database master = Sitecore.Data.Database.GetDatabase("master");

            //Get Media Library item by ID
            Item mediaLibraryItem = master.GetItem("{9E857E70-2F5D-4B68-A60C-C03135ACE0AC}");

            //Get the root item
            Item rootItem = master.GetItem(Sitecore.ItemIDs.ContentRoot);

            //List of all media IDs inside RTE
            List<string> lstMediaIDs;

            Sitecore.Data.ID sitecoreMediaID;

            Guid guidID;

            Item mediaItem;

            //Field Name for Media library items
            string strExtFieldName = "Extension";

            //Temp variable for storing the item Name/ID with it's original value
            string strNewValue = string.Empty;

            //Loop on all items under the root item
            foreach (Item item in rootItem.Axes.GetDescendants())
            {

                //Loop on all fields for each item
                foreach (Sitecore.Data.Fields.Field field in item.Fields)
                {

                    //Check if this field is RTE (rich text editor)
                    if (FieldTypeManager.GetField(field) is HtmlField && !field.Name.StartsWith("__"))
                    {

                        //check if the RTE contains andy media library item with '.ashx' extension
                        if (field.Value.ToLower().Contains(".ashx"))
                        {

                            //Get all IDs for all media items with '.ashx' extension
                            lstMediaIDs = GetAllMediaIDs(field.Value);

                            //Change the extension for every media item
                            foreach (string mediaName in lstMediaIDs)
                            {
                                //Check if Media item is referenced by Sitecore ID (Guid)
                                if (Guid.TryParse(mediaName, out guidID))
                                {
                                    sitecoreMediaID = new Sitecore.Data.ID(guidID);

                                    //Get the original item of this item from Media Libarary by 'ID'
                                    mediaItem = mediaLibraryItem.Axes.GetDescendants().Where(x => x.ID == sitecoreMediaID).FirstOrDefault();
                                }
                                //If the Media item is referenced by Name
                                else
                                {
                                    //Get the original item of this item from Media Libarary by 'Name'
                                    mediaItem = mediaLibraryItem.Axes.GetDescendants().Where(x => x.Name == mediaName).FirstOrDefault();
                                }

                                if (mediaItem != null && mediaItem.Fields[strExtFieldName] != null && !string.IsNullOrEmpty(mediaItem.Fields[strExtFieldName].Value))
                                {

                                    //Get the original extension for the media item
                                    strNewValue = mediaName + "." + mediaItem.Fields[strExtFieldName].Value;

                                    using (new Sitecore.SecurityModel.SecurityDisabler())
                                    {
                                        item.Editing.BeginEdit();

                                        try
                                        {

                                         /*
                                            Replacing the old value of media library item with a new value with the original extension
                                            For example: 
                                            Old value :/9E857E70-2F5D-4B68-A60C-C03135ACE0AC.ashx
                                            New value :/9E857E70-2F5D-4B68-A60C-C03135ACE0AC.jpg
                                         */
                                            field.Value = field.Value.Replace(mediaName + ".ashx", strNewValue);

                                            item.Editing.EndEdit();
                                        }
                                        catch (System.Exception ex)
                                        {
                                            Sitecore.Diagnostics.Log.Error("Could not update item " + item.Paths.FullPath + ": " + ex.Message, this);

                                            item.Editing.CancelEdit();
                                        }

                                    }
                                }

                            }

                        }
                    }
                }

                //Publishing item to 'web' database
                PublishItem(item);
            }

        }

        /// <summary>
        /// Publishing this item to 'web' database
        /// </summary>
        /// <param name="item">item to be published</param>
        private void PublishItem(Sitecore.Data.Items.Item item)
        {
            // The publishOptions determine the source and target database,
            // the publish mode and language, and the publish date
            Sitecore.Publishing.PublishOptions publishOptions =
              new Sitecore.Publishing.PublishOptions(item.Database,
                                                     Database.GetDatabase("web"),
                                                     Sitecore.Publishing.PublishMode.SingleItem,
                                                     item.Language,
                                                     System.DateTime.Now);  // Create a publisher with the publishoptions
            Sitecore.Publishing.Publisher publisher = new Sitecore.Publishing.Publisher(publishOptions);

            // Choose where to publish from
            publisher.Options.RootItem = item;

            // Publish children as well?
            publisher.Options.Deep = false;

            // Do the publish!
            publisher.Publish();
        }

        /// <summary>
        /// This Method return all matched strings as a list between forward slash '/' and '.ashx'
        /// </summary>
        /// <param name="strSource">Source string</param>
        /// <returns></returns>
        public List<string> GetAllMediaIDs(string strSource)
        {
            List<string> result = new List<string>();

            foreach (string item in Regex.Matches(strSource, @"([^/]+)(?=\.ashx)").Cast<Match>().Select(m => m.Value))
            {
                result.Add(item);
            }
            return result;
        }

    }
}
