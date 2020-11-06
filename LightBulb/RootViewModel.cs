using System;
using System.Threading.Tasks;
using System.Windows;
using LightBulb.Internal;
using LightBulb.Services;
using LightBulb.ViewModels.Components;
using LightBulb.ViewModels.Components.Settings;
using LightBulb.ViewModels.Dialogs;
using LightBulb.ViewModels.Framework;
using LightBulb.WindowsApi.Timers;
using Stylet;

namespace LightBulb.ViewModels
{
    public class RootViewModel : Screen, IDisposable
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly DialogManager _dialogManager;
        private readonly SettingsService _settingsService;

        private readonly ITimer _checkForUpdatesTimer;

        public CoreViewModel Core { get; }

        public RootViewModel(
            IViewModelFactory viewModelFactory,
            DialogManager dialogManager,
            SettingsService settingsService,
            UpdateService updateService)
        {
            _viewModelFactory = viewModelFactory;
            _dialogManager = dialogManager;
            _settingsService = settingsService;

            Core = viewModelFactory.CreateCoreViewModel();

            DisplayName = $"{App.Name} v{App.VersionString}";

            _checkForUpdatesTimer = Timer.Create(TimeSpan.FromHours(3),
                async () => await updateService.CheckPrepareUpdateAsync());
        }

        private async Task ShowGammaRangePromptAsync()
        {
            if (_settingsService.IsExtendedGammaRangeUnlocked)
                return;

            var message = $@"
{App.Name} 检测到此计算机未解锁扩展的伽玛范围。
这可能会导致该应用在某些颜色配置下无法正常工作。

单击 确定 以解锁伽玛范围。".Trim();

            var dialog = _viewModelFactory.CreateMessageBoxViewModel(
                "伽玛范围有限",
                message,
                "确定", "取消"
            );

            if (await _dialogManager.ShowDialogAsync(dialog) == true)
            {
                _settingsService.IsExtendedGammaRangeUnlocked = true;
                _settingsService.Save();
            }
        }

        private async Task ShowFirstTimeExperienceMessageAsync()
        {
            if (!_settingsService.IsFirstTimeExperienceEnabled)
                return;

            var message = $@"
感谢您安装 {App.Name}!
为了获得最个性化的体验，请在设置中配置您的地理位置.

单击 确定 来打开设置。".Trim();

            var dialog = _viewModelFactory.CreateMessageBoxViewModel(
                "欢迎!",
                message,
                "确定", "取消"
            );

            // Disable first time experience in the future
            _settingsService.IsFirstTimeExperienceEnabled = false;
            _settingsService.IsAutoStartEnabled = true;
            _settingsService.Save();

            if (await _dialogManager.ShowDialogAsync(dialog) == true)
            {
                var settingsDialog = _viewModelFactory.CreateSettingsViewModel();
                settingsDialog.ActivateTabByType<LocationSettingsTabViewModel>();

                await _dialogManager.ShowDialogAsync(settingsDialog);
            }
        }

        protected override void OnViewLoaded()
        {
            base.OnViewLoaded();

            _settingsService.Load();
            Refresh();

            _checkForUpdatesTimer.Start();
        }

        // This is a custom event that fires when the dialog host is loaded
        public async void OnViewFullyLoaded()
        {
            await ShowGammaRangePromptAsync();
            await ShowFirstTimeExperienceMessageAsync();
        }

        public async void ShowSettings() =>
            await _dialogManager.ShowDialogAsync(_viewModelFactory.CreateSettingsViewModel());

        public void ShowAbout() => ProcessEx.StartShellExecute(App.GitHubProjectUrl);

        public void Exit() => Application.Current.Shutdown();

        public void Dispose()
        {
            _checkForUpdatesTimer.Dispose();
        }
    }
}