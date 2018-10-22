using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Runtime;

namespace ServiceFabricQueuedServices.Services
{
	using System.Fabric.Description;
	using System.Net;

	public static class QueuedServiceInstanceListenerUtility
    {
	    /// <summary>
	    /// Creates a new <see cref="ServiceInstanceListener"/> capable of listening to a queue containing
	    /// WCF queued messages for the given <typeparamref name="TServiceContract"/>.
	    /// </summary>
	    /// <param name="wcfServiceObject">
	    /// The WCF service instance to be used for servicing requests. Must be a stateless service. Must not be null.
	    /// </param>
	    /// <param name="netMessagingBinding">
	    /// The binding to be used to listen to the queue. Must not be null.
	    /// </param>
	    /// <param name="configPackageName">
	    /// The name of the configuration package used to retrieve configuration values. If null, the default
	    /// value is "Config".
	    /// </param>
	    /// <param name="sectionName">
	    /// The name of the configuration section used to retrieve configuration values. If null, the default
	    /// value is "ServiceBus".
	    /// </param>
	    /// <param name="listenConnectionStringParameterName">
	    /// The name of the configuration value that provides a Service Bus connection string with Listen privileges.
	    /// If null, the default is "ListenConnectionString".
	    /// </param>
	    /// <param name="queueNameProvider">
	    /// A <see cref="Func{TResult}"/> returning the name of the queue to be used. If null, the default behavior
	    /// will be to use the name of <typeparamref name="TServiceContract"/>.
	    /// </param>
	    /// <typeparam name="TServiceContract">
	    /// The interface that bears a <see cref="ServiceContractAttribute"/> and <see cref="OperationContractAttribute"/>s
	    /// on its methods to denote it as a ServiceModel (WCF) service.
	    /// </typeparam>
	    /// <returns>
	    /// A new <see cref="ServiceInstanceListener"/> configured to listen to an Azure Service Bus for WCF
	    /// messages for the given <typeparamref name="TServiceContract"/> service contract. Guaranteed not to
	    /// be null.
	    /// </returns>
	    /// <exception cref="ArgumentNullException">
	    /// Thrown if <paramref name="wcfServiceObject"/> or <see cref="NetMessagingBinding"/> are null.
	    /// </exception>
	    /// <exception cref="InvalidOperationException">
	    /// Thrown if the endpoint for the Service Bus queue's containing Azure Service Bus namespace could
	    /// not be resolved from the connection string.
	    /// </exception>
	    public static ServiceInstanceListener CreateQueuedServiceBusListener<TServiceContract>(
			TServiceContract wcfServiceObject,
			NetMessagingBinding netMessagingBinding,
			string configPackageName = "Config",
			string sectionName = "ServiceBus",
			string listenConnectionStringParameterName = "ListenConnectionString",
			Func<string> queueNameProvider = null)
		{
			if (wcfServiceObject == null)
			{
				throw new ArgumentNullException(nameof(wcfServiceObject));
			}

			if (netMessagingBinding == null)
			{
				throw new ArgumentNullException(nameof(netMessagingBinding));
			}

			return new ServiceInstanceListener(context =>
			{
				ConfigurationProperty configurationProperty = context.CodePackageActivationContext.GetConfigurationPackageObject(configPackageName).Settings.Sections[sectionName].Parameters[listenConnectionStringParameterName];

				string listenConnectionString = configurationProperty.IsEncrypted ? new NetworkCredential("junk", configurationProperty.DecryptValue()).Password : configurationProperty.Value;

				ServiceBusConnectionStringBuilder builder = new ServiceBusConnectionStringBuilder(listenConnectionString);

				Uri endpointUri;

				try
				{
					endpointUri = builder.Endpoints.SingleOrDefault();
				}
				catch (InvalidOperationException)
				{
					throw new InvalidOperationException("More than one endpoint was detected in connection string");
				}

				if (endpointUri == null)
				{
					throw new InvalidOperationException("No endpoint was detected in connection string");
				}

				UriBuilder uriBuilder = new UriBuilder(endpointUri);

				var listener = new WcfCommunicationListener<TServiceContract>(
					wcfServiceObject: wcfServiceObject,
					serviceContext: context,

					//
					// The name of the endpoint configured in the ServiceManifest under the Endpoints section
					// that identifies the endpoint that the WCF ServiceHost should listen on.
					//
					address: new EndpointAddress(ServiceBusEnvironment.CreateServiceUri("sb", uriBuilder.Host.Split('.').First(), queueNameProvider == null ? typeof(TServiceContract).Name : queueNameProvider.Invoke())),

					//
					// Populate the binding information that you want the service to use.
					//
					listenerBinding: netMessagingBinding
				);

				ServiceEndpoint serviceEndpoint = listener.ServiceHost.Description.Endpoints.Last();

				serviceEndpoint.Behaviors.Add(new TransportClientEndpointBehavior()
				{
					TokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName: builder.SharedAccessKeyName, sharedAccessKey: builder.SharedAccessKey)
				});

				return listener;
			});
		}

	    /// <summary>
	    /// Creates a new <see cref="ServiceInstanceListener"/> capable of listening to a queue containing
	    /// WCF queued messages for the given <typeparamref name="TServiceContract"/>.
	    /// </summary>
	    /// <param name="wcfServiceObject">
	    /// The WCF service instance to be used for servicing requests. Must be a stateless service. Must not be null.
	    /// </param>
	    /// <param name="netMessagingBinding">
	    /// The binding to be used to listen to the queue. Must not be null.
	    /// </param>
	    /// <param name="configPackageName">
	    /// The name of the configuration package used to retrieve configuration values. If null, the default
	    /// value is "Config".
	    /// </param>
	    /// <param name="sectionName">
	    /// The name of the configuration section used to retrieve configuration values. If null, the default
	    /// value is "ServiceBus".
	    /// </param>
	    /// <param name="listenConnectionStringParameterName">
	    /// The name of the configuration value that provides a Service Bus connection string with Listen privileges.
	    /// If null, the default is "ListenConnectionString".
	    /// </param>
	    /// <param name="queueNameProvider">
	    /// A <see cref="Func{TResult}"/> returning the name of the queue to be used. If null, the default behavior
	    /// will be to use the name of <typeparamref name="TServiceContract"/>.
	    /// </param>
	    /// <typeparam name="TServiceContract">
	    /// The interface that bears a <see cref="ServiceContractAttribute"/> and <see cref="OperationContractAttribute"/>s
	    /// on its methods to denote it as a ServiceModel (WCF) service.
	    /// </typeparam>
	    /// <returns>
	    /// A new <see cref="ServiceInstanceListener"/> configured to listen to an Azure Service Bus for WCF
	    /// messages for the given <typeparamref name="TServiceContract"/> service contract. Guaranteed not to
	    /// be null.
	    /// </returns>
	    /// <exception cref="ArgumentNullException">
	    /// Thrown if <paramref name="wcfServiceObject"/> or <see cref="NetMessagingBinding"/> are null.
	    /// </exception>
	    /// <exception cref="InvalidOperationException">
	    /// Thrown if the endpoint for the Service Bus queue's containing Azure Service Bus namespace could
	    /// not be resolved from the connection string.
	    /// </exception>
	    public static ServiceInstanceListener CreateQueuedServiceBusListener<TServiceContract>(
			TServiceContract wcfServiceObject,
			NetMessagingBinding netMessagingBinding,
			string connectionString,
			Func<string> queueNameProvider = null)
		{
			if (wcfServiceObject == null)
			{
				throw new ArgumentNullException(nameof(wcfServiceObject));
			}

			if (netMessagingBinding == null)
			{
				throw new ArgumentNullException(nameof(netMessagingBinding));
			}

			if (String.IsNullOrEmpty(connectionString))
			{
				throw new ArgumentException("Must not be null or empty", nameof(connectionString));
			}

			return new ServiceInstanceListener(context =>
			{
				ServiceBusConnectionStringBuilder builder = new ServiceBusConnectionStringBuilder(connectionString);

				Uri endpointUri;

				try
				{
					endpointUri = builder.Endpoints.SingleOrDefault();
				}
				catch (InvalidOperationException)
				{
					throw new InvalidOperationException("More than one endpoint was detected in connection string");
				}

				if (endpointUri == null)
				{
					throw new InvalidOperationException("No endpoint was detected in connection string");
				}

				UriBuilder uriBuilder = new UriBuilder(endpointUri);

				var listener = new WcfCommunicationListener<TServiceContract>(
					wcfServiceObject: wcfServiceObject,
					serviceContext: context,

					//
					// The name of the endpoint configured in the ServiceManifest under the Endpoints section
					// that identifies the endpoint that the WCF ServiceHost should listen on.
					//
					address: new EndpointAddress(ServiceBusEnvironment.CreateServiceUri("sb", uriBuilder.Host.Split('.').First(), queueNameProvider == null ? typeof(TServiceContract).Name : queueNameProvider.Invoke())),

					//
					// Populate the binding information that you want the service to use.
					//
					listenerBinding: netMessagingBinding
				);

				ServiceEndpoint serviceEndpoint = listener.ServiceHost.Description.Endpoints.Last();

				serviceEndpoint.Behaviors.Add(new TransportClientEndpointBehavior()
				{
					TokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName: builder.SharedAccessKeyName, sharedAccessKey: builder.SharedAccessKey)
				});

				return listener;
			});
		}
	}
}
