using DrivingSchoolApp.DTOs.ValueObject;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Mapper.ValueObject;

public static class MoneyMapper
{
    extension(MoneyDto dto)
    {
        public Money ToModel()
        {
            return new Money(dto.Amount, dto.Currency);
        }
    }

    extension(Money model)
    {
        public MoneyDto ToDto()
        {
            return new MoneyDto(model.Amount, model.Currency);
        }
    }
}