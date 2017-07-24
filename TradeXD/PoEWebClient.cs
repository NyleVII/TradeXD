using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace TradeXD
{
    public class Profile
    {
        [JsonProperty("activeCharacter")] public Character ActiveCharacter;
        [JsonProperty("accountName")] public string Name;

        public override string ToString()
        {
            return $@"{Name}, {ActiveCharacter.Name}, {ActiveCharacter.League}";
        }
    }

    public class Character
    {
        [JsonProperty("league")] public string League;
        [JsonProperty("name")] public string Name;
    }

    // character-window/get-stash-items?tabs=1
    public class TabInfo
    {
        [JsonProperty("numTabs")] public int Count;
        [JsonProperty("tabs")] public Tab[] Tabs;
    }

    public class Tab
    {
        [JsonProperty("i")] public int Index;
        [JsonProperty("n")] public string Name;
        [JsonProperty("type")] public string Type;

        public override string ToString()
        {
            return $"[{Type}] {Name}";
        }
    }

    // character-window/get-stash-items?tabIndex=#
    public class ItemTab
    {
        [JsonProperty("numTabs")] public int Count;
        [JsonProperty("quadLayout")] public bool IsQuad;
        [JsonProperty("items")] public Item[] Items;
    }

    public class Item
    {
        [JsonProperty("typeLine")] public string BaseType;
        [JsonProperty("frameType")] public int FrameType;
        [JsonProperty("h")] public int Height;
        [JsonProperty("identified")] public bool Identified;
        [JsonProperty("ilvl")] public int Level;
        [JsonProperty("name")] public string Name;
        public Constants.ItemType Type;
        [JsonProperty("w")] public int Width;
        [JsonProperty("x")] public int X;
        [JsonProperty("y")] public int Y;

        public bool Is1By3 => Width == 1 && Height == 3;
        public bool Is2By3 => Width == 1 && Height == 3;

        public override string ToString()
        {
            return $"Item level {Level} {(Name == "" ? "" : Name + " ")}{BaseType}";
        }
    }

    internal class PoEWebClient : WebClient
    {
        private readonly CookieContainer _cookie = new CookieContainer();
        private string _csrfToken = "";
        private Profile _profile;

        internal string League => _profile == null ? "" : _profile.ActiveCharacter.League;

        internal PoEWebClient()
        {
            BaseAddress = PoEHelper.Uri;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
                (request as HttpWebRequest).CookieContainer = _cookie;
            return request;
        }

        internal bool GetProfile(string body)
        {
            const string needle1 = "function(C){var c = new C(";
            const string needle2 = "); c.render();});});    //-->";

            int start = body.IndexOf(needle1, StringComparison.Ordinal);
            int end = body.IndexOf(needle2, StringComparison.Ordinal);
            if (start == -1 || end == -1) return false;
            body = body.Substring(start + needle1.Length, end - start - needle1.Length);
            _profile = JsonConvert.DeserializeObject<Profile>(body);
            return _profile != null;
        }

        internal bool Login(string email, string password)
        {
            string body = DownloadString("login");
            const string needle = "name=\"hash\" value=\"";
            int index = body.IndexOf(needle, StringComparison.Ordinal);
            if (index == -1) return false;
            _csrfToken = body.Substring(index + needle.Length, 32);

            NameValueCollection loginData = new NameValueCollection
            {
                {"login_email", email},
                {"login_password", password},
                {"hash", _csrfToken},
                {"login", "Login"}
            };
            body = Encoding.UTF8.GetString(UploadValues("login", "POST", loginData));
            loginData["login_password"] = "";
            loginData = null;
            if (!body.Contains("Change avatar")) return false;
            return GetProfile(body);
        }

        internal Tab[] GetTabInfo()
        {
            var uri = "character-window/get-stash-items?";
            NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
            query.Add("accountName", _profile.Name);
            query.Add("league", _profile.ActiveCharacter.League);
            query.Add("tabs", "1");
            uri += query.ToString();
            string body = DownloadString(uri);
            return JsonConvert.DeserializeObject<TabInfo>(body).Tabs;
        }

        internal ItemTab GetTabItems(int index)
        {
            var uri = "character-window/get-stash-items?";
            NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
            query.Add("accountName", _profile.Name);
            query.Add("league", _profile.ActiveCharacter.League);
            query.Add("tabIndex", index.ToString());
            uri += query.ToString();
            string body = DownloadString(uri);

            var wrapper = JsonConvert.DeserializeObject<ItemTab>(body);
            foreach (Item i in wrapper.Items)
                if (!Constants.BaseItemType.TryGetValue(i.BaseType, out i.Type))
                    i.Type = Constants.ItemType.Unknown;
            return wrapper;
        }
    }
}