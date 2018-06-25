using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.Impl;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitmqNet
{
    public class Client
    {
        public Connection Connection { get; }

        public Client(Connection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
        /// <summary>
        /// 发送
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <param name="routingKey"></param>
        /// <param name="value"></param>
        public void Publish<TIn>(string routingKey, TIn value)
        {
            if (!Connection.IsConnected)
            {
                Connection.TryConnect();
            }
            //using (var channel = Connection.CreateModel())
            //{
            var channel = Connection.channel;
            channel.ExchangeDeclare(exchange: Connection.Exchange,
                                    type: "topic");

            var message = JsonConvert.SerializeObject(value);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: Connection.Exchange,
                               routingKey: routingKey,
                               basicProperties: null,
                               body: body);
            //}
        }
        /// <summary>
        /// 订阅
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="bindingKey"></param>
        /// <param name="isReply"></param>
        public void Subscribe<TIn, TOut>(string bindingKey, bool isReply = true) where TIn : IRabbitMQNetHandler<TOut>
        {
            if (!Connection.IsConnected)
            {
                Connection.TryConnect();
            }
            //using (var channel = Connection.CreateModel())
            //{
            var channel = Connection.channel;
            channel.ExchangeDeclare(exchange: Connection.Exchange, type: "topic");
            var queueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(queue: queueName,
                              exchange: Connection.Exchange,
                              routingKey: bindingKey);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var handler = Activator.CreateInstance<TIn>();
                var message = Encoding.UTF8.GetString(ea.Body);
                handler.Handle(JsonConvert.DeserializeObject<TOut>(message));
            };
            channel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);
            //}
        }
    }
}
