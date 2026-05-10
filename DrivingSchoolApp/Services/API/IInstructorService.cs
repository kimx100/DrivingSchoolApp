using DrivingSchoolApp.DTOs.Common;
using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.TheoryLesson;
using RestSharp;

namespace DrivingSchoolApp.Services.API;

public interface IInstructorService
{
    Task<RestResponse<InstructorDto>> RegisterInstructorAsync(InstructorRegistryDto registryDto);
    Task<RestResponse<List<InstructorDto>>> GetAllInstructorsAsync(bool checkCache = true);
    Task<RestResponse<InstructorDto>> GetInstructorByIdAsync(Guid instructorId, bool checkCache = true);
    Task<RestResponse<InstructorDto>> UpdateInstructorAsync(Guid instructorId, InstructorUpdateDto updateDto);
    Task<RestResponse> UpdateInstructorPasswordAsync(Guid instructorId, UpdatePasswordDto updateDto);
    Task<RestResponse> DeleteInstructorAsync(Guid instructorId);
    Task<RestResponse<TheoryLessonDto>> CreateTheoryLessonAsync(Guid instructorId, TheoryLessonRegistryDto registryDto);
    Task<RestResponse<List<TheoryLessonDto>>> GetTheoryLessonsAsync(Guid instructorId, bool checkCache = true);
    Task<RestResponse<DrivingLessonDto>> CreateDrivingLessonAsync(Guid instructorId,
        DrivingLessonRegistryDto registryDto);

    Task<RestResponse<List<DrivingLessonDto>>> GetDrivingLessonsAsync(Guid instructorId, bool checkCache = true);
}