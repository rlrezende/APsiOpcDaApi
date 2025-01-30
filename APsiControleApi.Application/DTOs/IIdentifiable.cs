using System;
using System.Collections.Generic;

namespace APsiControleApi.Application.DTOs
{
    public interface IIdentifiable
    {
         Guid Id { get; set; }
    }
}