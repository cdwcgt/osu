// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;
using osu.Game.Online.Notifications.WebSocket;
using osu.Game.Overlays.Notifications;
using osu.Game.Users;

namespace osu.Game.Tournament.Screens
{
    public partial class OsuGameScreen : TournamentScreen
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        private NestedOsuGame? nestedGame;

        public override void Show()
        {
            base.Show();
            nestedGame = new NestedOsuGame(host.Storage, new ForwardingAPIAccess(api))
            {
                Masking = true
            };
            nestedGame.SetHost(host);
            AddInternal(nestedGame);
        }

        public override void Hide()
        {
            if (nestedGame != null) RemoveInternal(nestedGame, true);

            base.Hide();
        }
    }

    public partial class NestedOsuGame : OsuGame
    {
        public NestedOsuGame(Storage storage, IAPIProvider api, string[]? args = null)
            : base(args)
        {
            Storage = storage;
            API = api;
        }

        protected override void Update()
        {
            base.Update();

            ((Bindable<bool>)IsActive).Value = true;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            LocalConfig.SetValue(OsuSetting.ShowFirstRunSetup, false);
            Notifications.Post(new SimpleNotification
            {
                Text = "不要用主菜单的退出按钮\n会退出整个比赛端"
            });
        }
    }

    public class ForwardingAPIAccess : IAPIProvider
    {
        private readonly IAPIProvider api;
        private readonly Bindable<UserActivity> activity = new Bindable<UserActivity>();

        public ForwardingAPIAccess(IAPIProvider api)
        {
            this.api = api;
        }

        public IBindable<APIUser> LocalUser => api.LocalUser;
        public IBindableList<APIUser> Friends => api.Friends;

        // Not using api.Activity because the tournament client MetadataClient has already bound to it
        public IBindable<UserActivity> Activity => activity;
        public IBindable<UserStatistics?> Statistics => api.Statistics;
        public Language Language => api.Language;
        public string AccessToken => api.AccessToken;
        public bool IsLoggedIn => api.IsLoggedIn;
        public string ProvidedUsername => api.ProvidedUsername;
        public string APIEndpointUrl => api.APIEndpointUrl;
        public string WebsiteRootUrl => api.WebsiteRootUrl;
        public int APIVersion => api.APIVersion;
        public Exception? LastLoginError => api.LastLoginError;
        public IBindable<APIState> State => api.State;

        public void Queue(APIRequest request)
        {
            api.Queue(request);
        }

        public void Perform(APIRequest request)
        {
            api.Perform(request);
        }

        public Task PerformAsync(APIRequest request)
        {
            return api.PerformAsync(request);
        }

        public void Login(string username, string password)
        {
            api.Login(username, password);
        }

        public void AuthenticateSecondFactor(string code)
        {
            api.AuthenticateSecondFactor(code);
        }

        public void Logout()
        {
            api.Logout();
        }

        public void UpdateStatistics(UserStatistics newStatistics)
        {
            api.UpdateStatistics(newStatistics);
        }

        public IHubClientConnector? GetHubConnector(string clientName, string endpoint, bool preferMessagePack = true)
        {
            return api.GetHubConnector(clientName, endpoint, preferMessagePack);
        }

        public INotificationsClient NotificationsClient => api.NotificationsClient;

        public IChatClient GetChatClient() => api.GetChatClient();

        public RegistrationRequest.RegistrationRequestErrors? CreateAccount(string email, string username, string password) => api.CreateAccount(email, username, password);
    }
}
