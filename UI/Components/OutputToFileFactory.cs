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

        // LiveSplit checks the feed at startup and offers any newer version listed there.
        // docs/releasing.md has the feed's rules.
        public string UpdateName => ComponentName;

        // Each file's path in the feed is relative to this: the release tag, then the asset name.
        public string UpdateURL => "https://github.com/lotsofs/LiveSplit.OutputToFile/releases/download/";

        public string XMLURL => "https://raw.githubusercontent.com/lotsofs/LiveSplit.OutputToFile/main/update.xml";

        // AssemblyVersion in AssemblyInfo.cs, always four parts, so a feed entry for this release isn't newer.
        public Version Version => typeof(OutputToFileFactory).Assembly.GetName().Version;
    }
}