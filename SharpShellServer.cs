﻿using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using SharpShell.Attributes;
using SharpShell.Diagnostics;
using SharpShell.ServerRegistration;

namespace SharpShell
{
    /// <summary>
    /// The SharpShellServer class is the base class for all SharpShellServers.
    /// It provides the core standard functionality - registration, unregistration,
    /// identity information (as required by ISharpShellServer), MEF contract inheritance
    /// and definitions of virtual functions that can be overriden by advanced users
    /// to hook into key points in Server Lifecycle.
    /// 
    /// Note that ALL derived classes will Export ISharpShellServer - this is a useful
    /// feature as it means that the ServerManager tool (and other tools) can interrogate
    /// assemblies via MEF to get information on servers they contain.
    /// </summary>
    [InheritedExport(typeof(ISharpShellServer))]
    public abstract class SharpShellServer : ISharpShellServer
    {
        /// <summary>
        /// The COM Register function. Called by regasm to register a COM server
        /// in the system. This function will register the server associations defined
        /// by the type's COMServerAssociation attributes.
        /// </summary>
        /// <param name="type">The type.</param>
        [ComRegisterFunction]
        internal static void Register(Type type)
        {
            //  Register the type, use the operating system architecture to determine
            //  what registration type to perform.
            DoRegister(type, Environment.Is64BitOperatingSystem ? RegistrationType.OS64Bit : RegistrationType.OS32Bit);
        }

        /// <summary>
        /// The COM Unregister function. Called by regasm to unregister a COM server
        /// in the system. This function will unregister the server associations defined
        /// by the type's COMServerAssociation attributes.
        /// </summary>
        /// <param name="type">The type.</param>
        [ComUnregisterFunction]
        internal static void Unregister(Type type)
        {
            //  Unregister the type, use the operating system architecture to determine
            //  what registration type to unregister.
            DoUnregister(type, Environment.Is64BitOperatingSystem ? RegistrationType.OS64Bit : RegistrationType.OS32Bit);
        }

        /// <summary>
        /// Actually performs registration. The ComRegisterFunction decorated method will call this function
        /// internally with the flag appropriate for the operating system processor architecture.
        /// However, this function can also be called manually if needed.
        /// </summary>
        /// <param name="type">The type of object to register, this must be a SharpShellServer derived class.</param>
        /// <param name="registrationType">Type of the registration.</param>
        internal static void DoRegister(Type type, RegistrationType registrationType)
        {
            //  Get the assoication data.
            var assocationAttributes = type.GetCustomAttributes(typeof(COMServerAssociationAttribute), true)
                .OfType<COMServerAssociationAttribute>().ToList();

            //  Get the server type.
            var serverType = ServerTypeAttribute.GetServerType(type);

            //  Register the server associations, if there are any.
            if (assocationAttributes.Any())
            {
                ServerRegistrationManager.RegisterServerAssociations(
                    type.GUID, serverType, type.Name, assocationAttributes, registrationType);
            }

            //  Execute the custom register function, if there is one.
            CustomRegisterFunctionAttribute.ExecuteIfExists(type, registrationType);
        }

        /// <summary>
        /// Actually performs unregistration. The ComUnregisterFunction decorated method will call this function
        /// internally with the flag appropriate for the operating system processor architecture.
        /// However, this function can also be called manually if needed.
        /// </summary>
        /// <param name="type">The type of object to unregister, this must be a SharpShellServer derived class.</param>
        /// <param name="registrationType">Type of the registration to unregister.</param>
        internal static void DoUnregister(Type type, RegistrationType registrationType)
        {
            //  Get the assoication data.
            var assocationAttributes = type.GetCustomAttributes(typeof(COMServerAssociationAttribute), true)
                .OfType<COMServerAssociationAttribute>().ToList();

            //  Get the server type.
            var serverType = ServerTypeAttribute.GetServerType(type);
            
            //  Unregister the server associations, if there are any.
            if (assocationAttributes.Any())
            {
                ServerRegistrationManager.UnregisterServerAssociations(
                    type.GUID, serverType, type.Name, assocationAttributes, registrationType);
            }

            //  Execute the custom unregister function, if there is one.
            CustomUnregisterFunctionAttribute.ExecuteIfExists(type, registrationType);
        }

        /// <summary>
        /// Logs the specified message to the SharpShell log, with the name of the type.
        /// </summary>
        /// <param name="message">The message.</param>
        protected virtual void Log(string message)
        {
            //  Log the message, but put our type name first.
            Logging.Log(DisplayName + ": " + message);
        }

        /// <summary>
        /// Logs the specified message to the SharpShell log as an error, with the name of the type.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        protected virtual void LogError(string message, Exception exception = null)
        {
            //  Log the error, but put our type name first.
            Logging.Error(DisplayName + ": " + message, exception);
        }

        /// <summary>
        /// Gets a display name for the server.
        /// If the [DisplayName] attribute is defined on the type, then the value
        /// of this attribute will be used. If not, then the type name will be used.
        /// </summary>
        /// <value>
        /// The name of the server.
        /// </value>
        public string DisplayName
        {
            get 
            {
                //  Return the display name if set, otherwise the type name.
                return DisplayNameAttribute.GetDisplayNameOrTypeName(GetType());
            }
        }

        /// <summary>
        /// Gets the type of the server.
        /// </summary>
        /// <value>
        /// The type of the server.
        /// </value>
        public ServerType ServerType
        {
            get
            {
                //  Just return the ServerTypeAttribute on the class (we must always have one somewhere in the hierarchy).
                return ServerTypeAttribute.GetServerType(GetType());
            }
        }

        /// <summary>
        /// Gets the server CLSID.
        /// </summary>
        public Guid ServerClsid
        {
            get { return GetType().GUID; }
        }
    }
}
