namespace Sitecore.Support.MediaFramework.Brightcove.Upload
{
  #region Usings

  using System;
  using System.Collections.Specialized;
  using System.IO;
  using Configuration;
  using Data;
  using Data.Items;
  using Globalization;
  using RestSharp;
  using RestSharp.Data;
  using Sitecore.MediaFramework;
  using Sitecore.MediaFramework.Brightcove.Entities;
  using Sitecore.MediaFramework.Brightcove.Security;
  using Sitecore.MediaFramework.Diagnostics;
  using Sitecore.MediaFramework.Upload;

  #endregion

  public class VideoUploader : Sitecore.MediaFramework.Brightcove.Upload.VideoUploader
  {
    protected override Item GetAccountItem(NameValueCollection parameters)
    {
      var itemId = new ID(GetAccountId(parameters));
      var itemLanguage = parameters["selected_language"];
      return Factory.GetDatabase(GetDatabase(parameters)).GetItem(itemId, Language.Parse(itemLanguage));
    }

    public override void Upload(NameValueCollection parameters, byte[] fileBytes)
    {
      var accountItem = GetAccountItem(parameters);
      if (accountItem == null) return;
      if (!ValidateFileExtension(parameters.Get(Constants.Upload.FileName)))
      {
        UpdateStatus(Guid.Empty, GetFileId(parameters), accountItem.ID.Guid, 0, Translate.Text(" File uploading is stopped. Please select a file one of the types: " + FileExtensions));
      }
      else
      {
        var entity = UploadInternal(parameters, fileBytes, accountItem);
        if (entity is CanceledVideo) return;
        if (entity != null)
        {
          var item2 = SyncItem(entity, accountItem);
          if (item2 != null)
          {
            UpdateStatus(item2.ID.Guid, GetFileId(parameters), accountItem.ID.Guid, 100);
            return;
          }
        }
        UpdateStatus(Guid.Empty, GetFileId(parameters), accountItem.ID.Guid, 0, Translate.Text("Uploading failed. Media Item cannot be created."));
      }
    }

    protected override object UploadInternal(NameValueCollection parameters, byte[] fileBytes, Item accountItem)
    {
      try
      {
        var fileName = GetFileName(parameters);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        var upload2 = new VideoToUpload
        {
          Name = fileNameWithoutExtension,
          ShortDescription = fileNameWithoutExtension
        };

        var propertyValue = upload2;
        var authenticator = new BrightcoveAthenticator(accountItem);
        var body = new PostData("create_video", authenticator, "video", propertyValue);
        var context = new RestContext(Sitecore.MediaFramework.Brightcove.Constants.SitecoreRestSharpService, authenticator);
        var restRequest = context.CreateRequest<PostData, ResultData<string>>(EntityActionType.Create, "update_data", body);
        restRequest.AddFile(fileNameWithoutExtension, fileBytes, fileName);
        restRequest.Timeout = 0x5265c00;
        var data = context.GetResponse<ResultData<string>>(restRequest).Data;

        return !string.IsNullOrEmpty(data?.Result) ? new Video { Id = data.Result, Name = fileNameWithoutExtension } : null;
      }
      catch (Exception exception)
      {
        LogHelper.Error("Brightcove Upload is failed", this, exception);
        return null;
      }
    }
  }
}
