using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.Impl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitmqNet
{
    public class Client
    {
        public Connection Connection { get; }
        public IBasicProperties Properties { get; set; }
        public string Exchange { get; set; } = "RabbitmqNet";

        public Client(Connection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="value">需要发送的消息</param>
        /// <param name="routingKey">路由Key</param>
        /// <param name="exchangeName">留空则使用默认的交换机</param>
        public Task Publish<TIn>(string routingKey, string exchangeName, TIn value)
        {
            var sendBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
            return Publish(routingKey, exchangeName, sendBytes);
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="sendBytes"></param>
        /// <param name="routingKey"></param>
        /// <param name="exchangeName"></param>
        public Task Publish(string routingKey, string exchangeName, byte[] sendBytes)
        {
            using (IModel channel = ExchangeDeclare(exchangeName ?? Exchange))
            {
                channel.BasicReturn += async (se, ex) =>
                await Task.Delay(Connection.NoConsumerMessageRetryInterval)
                .ContinueWith((t) => Publish(ex.RoutingKey, ex.Exchange, ex.Body));

                channel.BasicPublish(
                    exchange: exchangeName ?? Exchange,
                    routingKey: routingKey,
                    mandatory: true,
                    basicProperties: Properties,
                    body: sendBytes);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 订阅
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="bindingKey"></param>
        /// <param name="isReply"></param>
        public void Subscribe<TIn, TOut>(string bindingKey, string queueName, string exchangeName = "") where TIn : IRabbitMQNetHandler<TOut>
        {
            if (!Connection.IsConnected)
            {
                Connection.TryConnect();
            }
            var channel = QueueDeclare(exchangeName, queueName);

            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);


            channel.QueueBind(queue: queueName,
                                  exchange: exchangeName,
                                  routingKey: bindingKey);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var handler = Activator.CreateInstance<TIn>();
                var message = Encoding.UTF8.GetString(ea.Body);
                handler.Handle(JsonConvert.DeserializeObject<TOut>(message));
                channel.BasicAck(ea.DeliveryTag, false);
            };
            channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: consumer);
        }

        /// <summary>
        /// 声明队列
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        private IModel QueueDeclare(string exchangeName, string queueName = "")
        {
            IModel channel = ExchangeDeclare(exchangeName);
            if (string.IsNullOrWhiteSpace(queueName))
            {
                queueName = channel.QueueDeclare().QueueName;
                Properties = null;
            }
            else
            {
                channel.QueueDeclare(queue: queueName,
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);
                Properties = channel.CreateBasicProperties();
                Properties.Persistent = true;
            }
            return channel;
        }
        private IModel ExchangeDeclare(string exchangeName)
        {
            var channel = Connection.CreateModel();
            try
            {
                channel.ExchangeDeclarePassive(exchangeName ?? Exchange);
            }
            catch
            {
                channel = Connection.CreateModel();
                channel.ExchangeDeclare(exchange: exchangeName ?? Exchange,
                     type: "topic",
                    durable: true, autoDelete: false);

            }
            return channel;
        }
    }
}
