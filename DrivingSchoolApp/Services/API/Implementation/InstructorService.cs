using DrivingSchoolApp.DTOs.Common;
using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.TheoryLesson;
using DrivingSchoolApp.DTOs.ValueObject;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace DrivingSchoolApp.Services.API.Implementation;

public class InstructorService(IConfiguration config) : ApiService(config["api_base_url"]!), IInstructorService
{
    public async Task<RestResponse<InstructorDto>> RegisterInstructorAsync(InstructorRegistryDto registryDto)
    {
        var request = new RestRequest("/instructor/register", Method.Post);

        return await ExecuteRequestAsync<InstructorDto>(request);
    }

    public async Task<RestResponse<List<InstructorDto>>> GetAllInstructorsAsync()
    {
        var request = new RestRequest("/instructor");

        return await ExecuteRequestAsync<List<InstructorDto>>(request);
    }

    public async Task<RestResponse<InstructorDto>> GetInstructorByIdAsync(Guid instructorId)
    {
        var request = new RestRequest($"/instructor/{instructorId}");

        return await ExecuteRequestAsync<InstructorDto>(request);
    }

    public async Task<RestResponse<InstructorDto>> UpdateInstructorAsync(Guid instructorId, InstructorUpdateDto updateDto)
    {
        var request = new RestRequest($"/instructor/{instructorId}", Method.Put);

        request.AddJsonBody(updateDto);

        return await ExecuteRequestAsync<InstructorDto>(request);
    }

    public async Task<RestResponse> UpdateInstructorPasswordAsync(Guid instructorId, UpdatePasswordDto updateDto)
    {
        var request = new RestRequest($"/instructor/{instructorId}/password", Method.Put);

        request.AddJsonBody(updateDto);

        return await ExecuteRequestAsync(request);
    }

    public async Task<RestResponse> DeleteInstructorAsync(Guid instructorId)
    {
        var request = new RestRequest($"/instructor/{instructorId}", Method.Delete);

        return await ExecuteRequestAsync(request);
    }

    public async Task<RestResponse<TheoryLessonDto>> CreateTheoryLessonAsync(Guid instructorId, TheoryLessonRegistryDto registryDto)
    {
        var request = new RestRequest($"/instructor/{instructorId}/theoryLesson", Method.Post);
        
        request.AddParameter("LessonDateTime", registryDto.LessonDateTime);
        request.AddParameter("Price.Amount", registryDto.Price.Amount);
        request.AddParameter("Price.Currency", registryDto.Price.Currency);
        request.AddParameter("StudentId", registryDto.StudentId);
        
        // Signature
        var studentSignatureMs = new MemoryStream(registryDto.StudentSignature);
        var instructorSignatureMs = new MemoryStream(registryDto.InstructorSignature);
        request.AddFile("StudentSignature", () => studentSignatureMs, "filename");
        request.AddFile("InstructorSignature", () => instructorSignatureMs, "filename");

        request.AddHeader("Content-Type", "multipart/form-data");

        return await ExecuteRequestAsync<TheoryLessonDto>(request);
    }

    public async Task<RestResponse<List<TheoryLessonDto>>> GetTheoryLessonsAsync(Guid instructorId)
    {
        var request = new RestRequest($"/instructor/{instructorId}/theoryLesson");

        return await ExecuteRequestAsync<List<TheoryLessonDto>>(request);
    }

    public async Task<RestResponse<DrivingLessonDto>> CreateDrivingLessonAsync(Guid instructorId, DrivingLessonRegistryDto registryDto)
    {
        var request = new RestRequest($"/instructor/{instructorId}/drivingLesson", Method.Post);
        var studentSignatureMs = new MemoryStream(registryDto.StudentSignature);
        var instructorSignatureMs = new MemoryStream(registryDto.InstructorSignature);

        request.AddFile("StudentSignature", () => studentSignatureMs, "filename");
        request.AddFile("InstructorSignature", () => instructorSignatureMs, "filename");
        request.AddParameter("SchoolId", registryDto.SchoolId);
        request.AddParameter("StudentId", registryDto.StudentId);

        // Add route
        request.AddParameter("Route.DateTimeRange.StartDateTime", registryDto.Route.DateTimeRange.StartDateTime);
        request.AddParameter("Route.DateTimeRange.EndDateTime", registryDto.Route.DateTimeRange.EndDateTime);
        var coordinates = registryDto.Route.RouteCoordinates;
        for(int i = 0; i < registryDto.Route.RouteCoordinates.Length; i++)
        {
            request.AddParameter($"Route.RouteCoordinates[{i}].Order", coordinates[i].Order);
            request.AddParameter($"Route.RouteCoordinates[{i}].Latitude", coordinates[i].Latitude);
            request.AddParameter($"Route.RouteCoordinates[{i}].Longitude", coordinates[i].Longitude);
        }

        request.AddParameter("Price.Amount", registryDto.Price.Amount);
        request.AddParameter("Price.Currency", registryDto.Price.Currency);
        // Objective
        var completedObjectives = registryDto.CompletedObjectives;
        request.AddParameter("CompletedObjectives.Highway", completedObjectives.Highway);
        request.AddParameter("CompletedObjectives.Night", completedObjectives.Night);
        request.AddParameter("CompletedObjectives.ParallelParking", completedObjectives.ParallelParking);
        request.AddParameter("CompletedObjectives.ReverseAroundCorner", completedObjectives.ReverseAroundCorner);
        request.AddParameter("CompletedObjectives.RightOfWay", completedObjectives.RightOfWay);
        request.AddParameter("CompletedObjectives.ThreePointTurn", completedObjectives.ThreePointTurn);
        
        request.AddHeader("Content-Type", "multipart/form-data");

        return await ExecuteRequestAsync<DrivingLessonDto>(request);
    }

    public async Task<RestResponse<List<DrivingLessonDto>>> GetDrivingLessonsAsync(Guid instructorId)
    {
        var request = new RestRequest($"/instructor/{instructorId}/drivingLesson");

        return await ExecuteRequestAsync<List<DrivingLessonDto>>(request);
    }
}