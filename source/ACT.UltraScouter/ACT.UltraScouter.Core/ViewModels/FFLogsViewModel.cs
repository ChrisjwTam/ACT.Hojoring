using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;
using FFXIV.Framework.FFXIVHelper;
using TamanegiMage.FFXIV_MemoryReader.Model;

namespace ACT.UltraScouter.ViewModels
{
    public class FFLogsViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        public FFLogsViewModel() : this(null, null)
        {
        }

        public FFLogsViewModel(
            FFLogs config,
            TargetInfoModel model)
        {
            this.config = config ?? Settings.Instance.FFLogs;
            this.model = model ?? TargetInfoModel.Instance;

            this.RaisePropertyChanged(nameof(Config));
            this.RaisePropertyChanged(nameof(Model));

            this.Initialize();
        }

        public override void Initialize()
        {
        }

        private FFLogs config;
        private TargetInfoModel model;

        public virtual FFLogs Config => this.config;
        public virtual TargetInfoModel Model => this.model;

        public bool OverlayVisible
        {
            get
            {
                if (!this.Config.Visible)
                {
                    return false;
                }

                if (this.Config.IsDesignMode)
                {
                    return true;
                }

                if (string.IsNullOrEmpty(this.Config.ApiKey) ||
                    this.Model?.ObjectType != ObjectType.PC)
                {
                    return false;
                }

                if (this.Config.HideInCombat &&
                    FFXIVPlugin.Instance.InCombat)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
