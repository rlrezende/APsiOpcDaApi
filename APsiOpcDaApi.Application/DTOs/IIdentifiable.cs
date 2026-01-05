using System;
using System.Collections.Generic;

namespace APsiOpcDaApi.Application.DTOs
{
    public interface IIdentifiable
    {
         Guid Id { get; set; }
    }
}

