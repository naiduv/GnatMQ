/*
Copyright (c) 2013, 2014 Paolo Patierno

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution. 

The Eclipse Public License is available at 
   http://www.eclipse.org/legal/epl-v10.html
and the Eclipse Distribution License is available at 
   http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Paolo Patierno - initial API and implementation and/or initial documentation
*/

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace uPLibrary.Networking.M2Mqtt.Communication
{
    /// <summary>
    /// MQTT communication layer
    /// </summary>
    public class MqttTcpCommunicationLayer : IMqttCommunicationLayer
    {
        #region Constants ...

        // name for listener thread
        private const string LISTENER_THREAD_NAME = "MqttListenerThread";

        #endregion

        #region Properties ...

        /// <summary>
        /// TCP listening port
        /// </summary>
        public int Port { get; private set; }

        #endregion

        // TCP listener for incoming connection requests
        private TcpListener listener;

        // TCP listener thread
        private Thread thread;
        private bool isRunning;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="port">TCP listening port</param>
        public MqttTcpCommunicationLayer(int port)
        {
            this.Port = port;
        }

        #region IMqttCommunicationLayer ...

        // client connected event
        public event MqttClientConnectedEventHandler ClientConnected;

        /// <summary>
        /// Start communication layer listening
        /// </summary>
        public void Start()
        {
            this.isRunning = true;

            // create and start listener thread
            this.thread = new Thread(this.ListenerThread);
            this.thread.Name = LISTENER_THREAD_NAME;
            this.thread.Start();
        }

        /// <summary>
        /// Stop communication layer listening
        /// </summary>
        public void Stop()
        {
            this.isRunning = false;

            this.listener.Stop();

            // wait for thread
            this.thread.Join();
        }

        #endregion

        /// <summary>
        /// Listener thread for incoming connection requests
        /// </summary>
        private void ListenerThread()
        {
            // create listener...
            this.listener = new TcpListener(IPAddress.IPv6Any, this.Port);
            // set socket option 27 (IPV6_V6ONLY) to false to accept also connection on IPV4 (not only IPV6 as default)
            this.listener.Server.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
            // ...and start it
            this.listener.Start();

            while (this.isRunning)
            {
                try
                {
                    // blocking call to wait for client connection
                    Socket socketClient = this.listener.AcceptSocket();

                    // manage socket client connected
                    if (socketClient.Connected)
                    {
                        MqttClient client = new MqttClient(socketClient);
                        // raise client raw connection event
                        this.OnClientConnected(client);
                    }
                }
                catch (Exception)
                {
                    if (!this.isRunning)
                        return;
                }
            }
        }

        /// <summary>
        /// Raise client connected event
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnClientConnected(MqttClient client)
        {
            if (this.ClientConnected != null)
                this.ClientConnected(this, new MqttClientConnectedEventArgs(client));
        }
    }
}
