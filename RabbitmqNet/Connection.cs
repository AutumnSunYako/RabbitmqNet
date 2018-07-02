using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitmqNet
{
    public class Connection
    {
        /// <summary>
        /// rabbitmq连接字串：amqp://guest:guest@192.168.0.252:5672/
        /// </summary>
        public string ConnectionString { private set; get; }
        public string Exchange { get; set; } = "RabbitmqNet";
        public string ProvidedName { get; set; }
        public IConnection conn { get; set; }
        /// <summary>
        /// 无消费者时消息的重试时间(默认1秒)
        /// </summary>
        public TimeSpan NoConsumerMessageRetryInterval { set; get; }
        public bool IsConnected
        {
            get
            {
                return conn != null && conn.IsOpen;
            }
        }

        public Connection(string connectionString, string providedName)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            ProvidedName = providedName ?? throw new ArgumentNullException(nameof(providedName));
            TryConnect();
            NoConsumerMessageRetryInterval = TimeSpan.FromSeconds(1);
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            var channel = conn.CreateModel();
            return channel;
        }
        public void TryConnect()
        {
            if (!IsConnected)
            {
                ConnectionFactory factory = new ConnectionFactory();
                factory.Uri = new Uri(ConnectionString);
                conn = factory.CreateConnection(clientProvidedName: ProvidedName);
            }
        }
    }
}
