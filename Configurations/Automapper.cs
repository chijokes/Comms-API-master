using AutoMapper;
using FusionComms.DTOs;
using FusionComms.Entities;

namespace FusionComms.Configurations
{
    public class Automapper : Profile
    {
        public Automapper()
        {
            CreateMap<CreateUserDto, User>();
            CreateMap<AddMontyUser, RegisteredMontyUser>();
            CreateMap<AddSesUser, RegisteredSesUser>();
        }
    }
}
