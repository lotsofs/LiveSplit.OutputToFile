using LiveSplit.Model;
using System;

namespace LiveSplit.UI.Components.OutputToFile
{
    public class OutputToFileFactory : IComponentFactory
    {
        // The displayed name of the component in the Layout Editor.
        public string ComponentName => "Output to File";

        public string Description => "Outputs split information to a text file, which can then be added to OBS as a text source";

        // The sub-menu this component will appear under when adding the component to the layout.
        public ComponentCategory Category => ComponentCategory.Other;

        public IComponent Create(LiveSplitState state) => new OutputToFileComponent(state);

        public string UpdateName => ComponentName;

        public string UpdateURL => "";

        public string XMLURL => UpdateURL + "";

        public Version Version => Version.Parse("1.0.0");
    }
}