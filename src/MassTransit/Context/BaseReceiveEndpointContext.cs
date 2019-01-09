﻿// Copyright 2007-2018 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Context
{
    using System;
    using Configuration;
    using GreenPipes;
    using Pipeline;
    using Pipeline.Observables;
    using Topology;
    using Transports;


    public abstract class BaseReceiveEndpointContext :
        BasePipeContext,
        ReceiveEndpointContext
    {
        readonly IPublishTopologyConfigurator _publishTopology;
        readonly Lazy<IPublishEndpointProvider> _publishEndpointProvider;
        readonly Lazy<IPublishPipe> _publishPipe;
        readonly Lazy<IReceivePipe> _receivePipe;
        readonly Lazy<ISendEndpointProvider> _sendEndpointProvider;
        readonly Lazy<ISendPipe> _sendPipe;
        readonly Lazy<IMessageSerializer> _serializer;
        readonly Lazy<ISendTransportProvider> _sendTransportProvider;
        readonly Lazy<IPublishTransportProvider> _publishTransportProvider;
        protected readonly PublishObservable PublishObservers;
        protected readonly SendObservable SendObservers;
        readonly ReceiveObservable _receiveObservers;
        readonly ReceiveTransportObservable _transportObservers;
        readonly ReceiveEndpointObservable _endpointObservers;

        protected BaseReceiveEndpointContext(IReceiveEndpointConfiguration configuration)
        {
            InputAddress = configuration.InputAddress;
            HostAddress = configuration.HostAddress;

            _publishTopology = configuration.Topology.Publish;

            SendObservers = new SendObservable();
            PublishObservers = new PublishObservable();

            _endpointObservers = configuration.EndpointObservers;
            _receiveObservers = configuration.ReceiveObservers;
            _transportObservers = configuration.TransportObservers;

            _sendPipe = new Lazy<ISendPipe>(() => configuration.Send.CreatePipe());
            _publishPipe = new Lazy<IPublishPipe>(() => configuration.Publish.CreatePipe());
            _receivePipe = new Lazy<IReceivePipe>(configuration.CreateReceivePipe);

            _serializer = new Lazy<IMessageSerializer>(() => configuration.Serialization.Serializer);
            _sendEndpointProvider = new Lazy<ISendEndpointProvider>(CreateSendEndpointProvider);
            _publishEndpointProvider = new Lazy<IPublishEndpointProvider>(CreatePublishEndpointProvider);
            _sendTransportProvider = new Lazy<ISendTransportProvider>(CreateSendTransportProvider);
            _publishTransportProvider = new Lazy<IPublishTransportProvider>(CreatePublishTransportProvider);
        }

        protected IPublishPipe PublishPipe => _publishPipe.Value;
        public ISendPipe SendPipe => _sendPipe.Value;
        public IMessageSerializer Serializer => _serializer.Value;

        protected Uri HostAddress { get; }

        IReceiveObserver ReceiveEndpointContext.ReceiveObservers => _receiveObservers;

        IReceiveTransportObserver ReceiveEndpointContext.TransportObservers => _transportObservers;

        IReceiveEndpointObserver ReceiveEndpointContext.EndpointObservers => _endpointObservers;

        ConnectHandle ISendObserverConnector.ConnectSendObserver(ISendObserver observer)
        {
            return SendObservers.Connect(observer);
        }

        ConnectHandle IPublishObserverConnector.ConnectPublishObserver(IPublishObserver observer)
        {
            return PublishObservers.Connect(observer);
        }

        ConnectHandle IReceiveTransportObserverConnector.ConnectReceiveTransportObserver(IReceiveTransportObserver observer)
        {
            return _transportObservers.Connect(observer);
        }

        ConnectHandle IReceiveObserverConnector.ConnectReceiveObserver(IReceiveObserver observer)
        {
            return _receiveObservers.Connect(observer);
        }

        ConnectHandle IReceiveEndpointObserverConnector.ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
        {
            return _endpointObservers.Connect(observer);
        }

        public Uri InputAddress { get; }

        IPublishTopology ReceiveEndpointContext.Publish => _publishTopology;

        public IReceivePipe ReceivePipe => _receivePipe.Value;

        public ISendEndpointProvider SendEndpointProvider => _sendEndpointProvider.Value;
        public IPublishEndpointProvider PublishEndpointProvider => _publishEndpointProvider.Value;

        protected virtual ISendEndpointProvider CreateSendEndpointProvider()
        {
            return new SendEndpointProvider(_sendTransportProvider.Value, SendObservers, Serializer, InputAddress, SendPipe);
        }

        protected virtual IPublishEndpointProvider CreatePublishEndpointProvider()
        {
            return new PublishEndpointProvider(_publishTransportProvider.Value, HostAddress, PublishObservers, Serializer, InputAddress, PublishPipe,
                _publishTopology);
        }

        protected abstract ISendTransportProvider CreateSendTransportProvider();
        protected abstract IPublishTransportProvider CreatePublishTransportProvider();

        protected ISendTransportProvider SendTransportProvider => _sendTransportProvider.Value;
    }
}