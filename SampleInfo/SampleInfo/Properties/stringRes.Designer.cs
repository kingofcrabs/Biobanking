﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SampleInfo.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class stringRes {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal stringRes() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SampleInfo.Properties.stringRes", typeof(stringRes).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to \Data.
        /// </summary>
        internal static string dataFolder {
            get {
                return ResourceManager.GetString("dataFolder", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to \DoNucleinExtraction.txt.
        /// </summary>
        internal static string DoNucleinExtractionFile {
            get {
                return ResourceManager.GetString("DoNucleinExtractionFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to \hasBuffy.txt.
        /// </summary>
        internal static string hasBuffyFile {
            get {
                return ResourceManager.GetString("hasBuffyFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to maxSampleCount.
        /// </summary>
        internal static string maxSampleCount {
            get {
                return ResourceManager.GetString("maxSampleCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to \SampleCount.txt.
        /// </summary>
        internal static string SampleCountFile {
            get {
                return ResourceManager.GetString("SampleCountFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 0.10.
        /// </summary>
        internal static string Version {
            get {
                return ResourceManager.GetString("Version", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to xmlFolder.
        /// </summary>
        internal static string xmlFolder {
            get {
                return ResourceManager.GetString("xmlFolder", resourceCulture);
            }
        }
    }
}
