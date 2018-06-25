using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitmqNet
{
    public interface IRabbitMQNetHandler<TIn>
    {
        Task Handle(TIn obj);
    }
}
