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
        public string Exchange { get; set; }
        public IConnection conn { get; set; }
        public IModel channel { get; set; }
        public EventingBasicConsumer Consumer { get; set; }
        public bool IsConnected
        {
            get
            {
                return conn != null && conn.IsOpen;
            }
        }

        public Connection(string connectionString, string exchange)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            Exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri(connectionString);
            conn = factory.CreateConnection();
            channel = CreateModel();
        }
        public void Close()
        {
            channel.Close();
            conn.Close();
        }
        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return conn.CreateModel();
        }
        public void TryConnect()
        {
            if (!IsConnected)
            {
                ConnectionFactory factory = new ConnectionFactory();
                factory.Uri = new Uri(ConnectionString);
                conn = factory.CreateConnection();
                channel = CreateModel();
            }
        }
    }
}
