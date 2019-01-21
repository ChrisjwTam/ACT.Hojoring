using System;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    [Serializable]
    [DataContract(Namespace = "")]
    public class Enmity :
        BindableBase
    {
        [DataMember]
        public DisplayText DisplayText { get; set; } = new DisplayText();

        [DataMember]
        public Location Location { get; set; } = new Location();

        private bool visible;

        [DataMember]
        public bool Visible
        {
            get => this.visible;
            set => this.SetProperty(ref this.visible, value);
        }

        private bool hideInNotCombat = true;

        [DataMember]
        public bool HideInNotCombat
        {
            get => this.hideInNotCombat;
            set => this.SetProperty(ref this.hideInNotCombat, value);
        }

        private bool hideInSolo = true;

        [DataMember]
        public bool HideInSolo
        {
            get => this.hideInSolo;
            set => this.SetProperty(ref this.hideInSolo, value);
        }

        private double scale = 1.0d;

        [DataMember]
        public double Scale
        {
            get => this.scale;
            set => this.SetProperty(ref this.scale, value);
        }

        private int maxCountOfDisplay = 8;

        [DataMember]
        public int MaxCountOfDisplay
        {
            get => this.maxCountOfDisplay;
            set => this.SetProperty(ref this.maxCountOfDisplay, value);
        }

        private Color background = Colors.Transparent;

        [DataMember]
        public Color Background
        {
            get => this.background;
            set => this.SetProperty(ref this.background, value);
        }

        private bool isDesignMode;

        [XmlIgnore]
        public bool IsDesignMode
        {
            get => this.isDesignMode;
            set => this.SetProperty(ref this.isDesignMode, value);
        }
    }
}
