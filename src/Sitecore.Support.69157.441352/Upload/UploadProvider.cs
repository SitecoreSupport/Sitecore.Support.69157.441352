namespace Sitecore.Support.MediaFramework.Upload
{
  using System;
  using System.Collections.Specialized;
  using System.IO;
  using System.Web;
  using Globalization;
  using Jobs;
  using Newtonsoft.Json;
  using Sitecore.MediaFramework.Diagnostics;
  using Sitecore.MediaFramework.Upload;

  public class UploadProvider : Sitecore.MediaFramework.Upload.UploadProvider
  {
    protected override string Upload(HttpContext context)
    {
      context.Response.ContentType = "text/plain";
      var file = new UploadingFile
      {
        Size = 0,
        ThumbnailUrl = string.Format(Sitecore.MediaFramework.Constants.ErrorImage, 80, 80),
        ID = Guid.NewGuid()
      };
      try
      {
        if (context.Request.Files.Count == 0)
        {
          file.Name = Translate.Text("No Files");
          file.Error = Translate.Text("Empty file upload result!");
          LogHelper.Warn("Upload is Stopped. Request does not contain any file", this);
        }
        else
        {
          var accountList = this.GetAccountList(context);
          if (!this.VerifyList(accountList))
          {
            file.Error = Translate.Text("Account was not selected!");
            LogHelper.Warn("Upload is Stopped. Account was not selected!", this);
          }
          else
          {
            byte[] buffer;
            var file2 = context.Request.Files[0];
            var inputStream = file2.InputStream;
            var contentLength = file2.ContentLength;
            using (var stream2 = new MemoryStream())
            {
              inputStream.CopyTo(stream2);
              buffer = stream2.ToArray();
            }
            foreach (var account in accountList)
            {
              var values2 = new NameValueCollection
              {
                {Sitecore.MediaFramework.Constants.Upload.FileName, Path.GetFileName(file2.FileName)},
                {Sitecore.MediaFramework.Constants.Upload.FileId, file.ID.ToString()},
                {Sitecore.MediaFramework.Constants.Upload.AccountId, account.ID.ToString()},
                {Sitecore.MediaFramework.Constants.Upload.AccountTemplateId, account.AccountTemplateId.ToString()},
                {
                  Sitecore.MediaFramework.Constants.Upload.Database,
                  context.Request.QueryString.Get(Sitecore.MediaFramework.Constants.Upload.Database)
                }
              };

              var lang = HttpContext.Current.Cache["selectedlanguagetocreatemediaitem"];
              if (lang != null)
              {
                values2.Add("selected_language", (string) lang);
              }
              
              var properties = values2;
              var process = new UploadProcess(properties, buffer);
              var options = new JobOptions($"MediaFramework_Upload_{account.ID}_{file.ID}", "MediaFramework", Context.Site.Name, process, "Execute");
              var job = new Job(options);
              JobManager.Start(job);
            }
            file.Name = file2.FileName;
            file.Size = contentLength;
            file.ThumbnailUrl = string.Format(Sitecore.MediaFramework.Constants.DefaultPreview, 80, 80);
          }
        }
        return JsonConvert.SerializeObject(file);
      }
      catch (Exception exception)
      {
        LogHelper.Error("Upload is failed.", this, exception);
        return JsonConvert.SerializeObject(file);
      }
    }
  }
}
