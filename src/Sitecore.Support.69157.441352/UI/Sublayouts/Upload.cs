namespace Sitecore.Support.MediaFramework.UI.Sublayouts
{
  #region Usings

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Web;
  using System.Web.UI;
  using System.Web.UI.HtmlControls;
  using System.Web.UI.WebControls;
  using Data;
  using Data.Items;
  using Globalization;
  using Newtonsoft.Json;
  using Sitecore.MediaFramework;
  using Sitecore.MediaFramework.Account;
  using Sitecore.MediaFramework.Common;
  using Sitecore.MediaFramework.Diagnostics;

  #endregion

  public class Upload : Page
  {
    protected HiddenField AccountMenuWarn;
    protected HiddenField Buffered;
    protected HiddenField EmptyResult;
    protected HtmlForm form1;
    protected Literal ltrAccounts;
    protected Literal ltrAddFiles;
    protected Literal ltrBuffering;
    protected Literal ltrCancel;
    protected Literal ltrError;
    protected Literal ltrStart;
    protected Literal ltrStartUpload;
    protected HiddenField PageData;
    protected HiddenField TextGoTo;
    protected HiddenField TextSelectItem;
    protected HiddenField wrongExtension;

    protected void Page_Load(object sender, EventArgs e)
    {
      if (Page.IsPostBack)
        return;
      wrongExtension.Value = Translate.Text(" File uploading is stopped. Please select a file one of the types: ");
      TextGoTo.Value = Translate.Text("GO TO ITEM");
      TextSelectItem.Value = Translate.Text("SELECT ITEM");
      AccountMenuWarn.Value = Translate.Text("Provider's Account is not selected!");
      EmptyResult.Value = Translate.Text("Empty file upload result!");
      Buffered.Value = Translate.Text("Buffered");
      ltrAccounts.Text = Translate.Text("Accounts");
      ltrAddFiles.Text = Translate.Text("Add files...");
      ltrStartUpload.Text = Translate.Text("Start All");
      ltrStart.Text = Translate.Text("Start");
      ltrError.Text = Translate.Text("Error");
      ltrBuffering.Text = Translate.Text("Buffering");
      ltrCancel.Text = Translate.Text("Cancel buffering");
      PageData.Value = GetAccountData();
    }

    protected virtual Item GetAccountItem()
    {
      var str = Page.Request.QueryString.Get("itemId");
      var databaseName = Page.Request.QueryString.Get("database");
      if (!Data.ID.IsID(str) || string.IsNullOrEmpty(databaseName)) return null;
      var children = Database.GetDatabase(databaseName).GetItem(str);
      return children == null ? null : AccountManager.GetAccountItemForDescendant(children);
    }

    protected virtual string GetAccountData()
    {
      var str = Request.QueryString.Get("type");
      var databaseName = Page.Request.QueryString.Get("database");
      var database = string.IsNullOrEmpty(databaseName) ? Sitecore.Context.ContentDatabase ?? Sitecore.Context.Database : Database.GetDatabase(databaseName);
      var accountItem = GetAccountItem();
      var list1 = AccountManager.GetAllAccounts(database).Where(AccountManager.IsValidAccount).ToList();
      if (list1.Count == 0) LogHelper.Warn("Media Framework has no Accounts!", this, null);
      var list2 = list1.Select(acc => SetAccountProperties(acc, accountItem != null && acc.ID == accountItem.ID)).GroupBy(it => it.AccountTemplateId).Where(t => CheckAccount(t.Key)).ToList();

      var lang = HttpContext.Current.Cache["selectedlanguagetocreatemediaitem"];

      return JsonConvert.SerializeObject(new PageProperties
      {
        NoAcc = (accountItem == null),
        AllAccounts = list2,
        Database = database.Name,
        Mode = (string.IsNullOrEmpty(str) ? "embed" : str),
        Language = lang?.ToString() ?? "en"
      });
    }

    protected virtual Account SetAccountProperties(Item account, bool selected)
    {
      return new Account(account, selected);
    }

    protected virtual bool CheckAccount(Guid accountTemplateId)
    {
      return MediaFrameworkContext.GetUploadExecuter(new ID(accountTemplateId)) != null;
    }

    public class PageProperties
    {
      [JsonProperty("noAcc")]
      public bool NoAcc { get; set; }

      [JsonProperty("database")]
      public string Database { get; set; }

      [JsonProperty("allAccounts")]
      public List<IGrouping<Guid, Account>> AllAccounts { get; set; }

      [JsonProperty("mode")]
      public string Mode { get; set; }

      [JsonProperty("language")]
      public string Language { get; set; }
    }
  }
}
