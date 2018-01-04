﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.ServiceFabric.Services.Remoting {
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
    internal class SR {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SR() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.ServiceFabric.Services.Remoting.SR", typeof(SR).Assembly);
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
        ///   Looks up a localized string similar to Invalid Client for remoting..
        /// </summary>
        internal static string Error_InvalidOperation {
            get {
                return ResourceManager.GetString("Error_InvalidOperation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to CallBack Channel Not Found for this ClientId  : &apos;{0}&apos;.
        /// </summary>
        internal static string ErrorClientCallbackChannelNotFound {
            get {
                return ResourceManager.GetString("ErrorClientCallbackChannelNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to deserialize and get remote exception  {0}.
        /// </summary>
        internal static string ErrorDeserializationFailure {
            get {
                return ResourceManager.GetString("ErrorDeserializationFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The exception {0} was unhandled on the service and could not be serialized for transferring to the client..
        /// </summary>
        internal static string ErrorExceptionSerializationFailed1 {
            get {
                return ResourceManager.GetString("ErrorExceptionSerializationFailed1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Detailed Exception Information: {0}.
        /// </summary>
        internal static string ErrorExceptionSerializationFailed2 {
            get {
                return ResourceManager.GetString("ErrorExceptionSerializationFailed2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Header with name &apos;{0}&apos; already exists.
        /// </summary>
        internal static string ErrorHeaderAlreadyExists {
            get {
                return ResourceManager.GetString("ErrorHeaderAlreadyExists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Interface id &apos;{0}&apos; is not implemented by object &apos;{1}&apos;.
        /// </summary>
        internal static string ErrorInterfaceNotImplemented {
            get {
                return ResourceManager.GetString("ErrorInterfaceNotImplemented", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client is trying to connect to invalid address {0}..
        /// </summary>
        internal static string ErrorInvalidAddress {
            get {
                return ResourceManager.GetString("ErrorInvalidAddress", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The object of type &apos;{0}&apos; does support the method &apos;{1}&apos;.
        /// </summary>
        internal static string ErrorMethodNotImplemented {
            get {
                return ResourceManager.GetString("ErrorMethodNotImplemented", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The service type &apos;{0}&apos; does not implement any service interfaces or one of the interfaces implemented is not a service interface. All interfaces(including its parent interface) implemented by service type must be service interface. A service interface is the one that ultimately derives from &apos;{1}&apos; type..
        /// </summary>
        internal static string ErrorNoServiceInterfaceFound {
            get {
                return ResourceManager.GetString("ErrorNoServiceInterfaceFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The type &apos;{0}&apos; is not an service interface as it does not derive from the interface &apos;{1}&apos;..
        /// </summary>
        internal static string ErrorNotAServiceInterface_DerivationCheck1 {
            get {
                return ResourceManager.GetString("ErrorNotAServiceInterface_DerivationCheck1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The type &apos;{0}&apos; is not an service interface as it derive from a non service interface &apos;{1}&apos;. All service interfaces must derive from &apos;{2}&apos;..
        /// </summary>
        internal static string ErrorNotAServiceInterface_DerivationCheck2 {
            get {
                return ResourceManager.GetString("ErrorNotAServiceInterface_DerivationCheck2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The type &apos;{0}&apos; is not a service interface as it is not an interface. .
        /// </summary>
        internal static string ErrorNotAServiceInterface_InterfaceCheck {
            get {
                return ResourceManager.GetString("ErrorNotAServiceInterface_InterfaceCheck", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The  {0} interface &apos;{1}&apos; is using generics. Generic interfaces cannot be remoted..
        /// </summary>
        internal static string ErrorRemotedInterfaceIsGeneric {
            get {
                return ResourceManager.GetString("ErrorRemotedInterfaceIsGeneric", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{1}&apos; of {0} interface &apos;{2}&apos; has &apos;{4}&apos; parameter &apos;{3}&apos;, and it is not the last parameter. If a method of the {0} interface has parameter of type &apos;{4}&apos; it must be the last parameter..
        /// </summary>
        internal static string ErrorRemotedMethodCancellationTokenOutOfOrder {
            get {
                return ResourceManager.GetString("ErrorRemotedMethodCancellationTokenOutOfOrder", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{1}&apos; of {0} interface &apos;{2}&apos; does not return Task or Task&lt;&gt;. The {0} interface methods must be async and must return either Task or Task&lt;&gt;..
        /// </summary>
        internal static string ErrorRemotedMethodDoesNotReturnTask {
            get {
                return ResourceManager.GetString("ErrorRemotedMethodDoesNotReturnTask", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{1}&apos; of {0} interface &apos;{2}&apos; returns &apos;{3}&apos;. The {0} interface methods must have a return of type &apos;{4}&apos;..
        /// </summary>
        internal static string ErrorRemotedMethodDoesNotReturnVoid {
            get {
                return ResourceManager.GetString("ErrorRemotedMethodDoesNotReturnVoid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{1}&apos; of {0} interface &apos;{2}&apos; is using generics. The {0} interface methods cannot use generics..
        /// </summary>
        internal static string ErrorRemotedMethodHasGenerics {
            get {
                return ResourceManager.GetString("ErrorRemotedMethodHasGenerics", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{1}&apos; of {0} interface &apos;{2}&apos; has out/ref/optional parameter &apos;{3}&apos;. The {0} interface methods must not have out, ref or optional parameters..
        /// </summary>
        internal static string ErrorRemotedMethodHasOutRefOptionalParameter {
            get {
                return ResourceManager.GetString("ErrorRemotedMethodHasOutRefOptionalParameter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{1}&apos; of {0} interface &apos;{2}&apos; has variable length parameter &apos;{3}&apos;. The {0} interface methods must not have variable length parameters..
        /// </summary>
        internal static string ErrorRemotedMethodHasVaArgParameter {
            get {
                return ResourceManager.GetString("ErrorRemotedMethodHasVaArgParameter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{1}&apos; of {0} interface &apos;{2}&apos; is using a variable argument list. The {0} interface methods cannot have a variable argument list..
        /// </summary>
        internal static string ErrorRemotedMethodHasVaArgs {
            get {
                return ResourceManager.GetString("ErrorRemotedMethodHasVaArgs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{1}&apos; of {0} interface &apos;{2}&apos; is overloaded. The {0} interface methods cannot be overloaded..
        /// </summary>
        internal static string ErrorRemotedMethodsIsOverloaded {
            get {
                return ResourceManager.GetString("ErrorRemotedMethodsIsOverloaded", resourceCulture);
            }
        }
    }
}
