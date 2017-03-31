namespace Sitecore.Support.MediaFramework.Commands
{
  #region Usings

  using System;
  using System.Linq;
  using System.Web;
  using Data;
  using Links;
  using Shell.Framework;
  using Shell.Framework.Commands;
  using Sitecore.MediaFramework;
  using Sitecore.MediaFramework.Diagnostics;
  using Text;

  #endregion

  public class OpenUploader : Sitecore.MediaFramework.Commands.OpenUploader
  {
    public override void Execute(CommandContext context)
    {
      var item = Database.GetDatabase("core").GetItem(ItemIDs.UploadApplication);
      var str = new UrlString(LinkManager.GetItemUrl(item));
      var item2 = context.Items.FirstOrDefault();

      if (item2 != null)
      {
        HttpContext.Current.Cache["selectedlanguagetocreatemediaitem"] = item2.Language.Name;

        str.Parameters.Add("itemId", item2.ID.Guid.ToString());
        str.Parameters.Add("database", item2.Database.Name);
        str.Parameters.Add("type", "norm");
        str.Parameters.Add("language", item2.Language.Name);
      }
      try
      {
        Windows.RunApplication(item, item.Appearance.Icon, item.DisplayName, str.ToString());
      }
      catch (Exception exception)
      {
        LogHelper.Error("Opening Uploader failed.", this, exception);
      }
    }
  }
}
