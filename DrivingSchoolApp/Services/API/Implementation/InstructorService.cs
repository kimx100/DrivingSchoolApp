using DrivingSchoolApp.DTOs.Common;
using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.TheoryLesson;
using DrivingSchoolApp.DTOs.ValueObject;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace DrivingSchoolApp.Services.API.Implementation;

public class InstructorService : ApiService, IInstructorService
{
    private readonly IMemoryCache _cache;
    
    public InstructorService(IConfiguration config, IMemoryCache cache) : base(config["api_base_url"]!)
    {
        _cache = cache;
    }
    
    public async Task<RestResponse<InstructorDto>> RegisterInstructorAsync(InstructorRegistryDto registryDto)
    {
        var request = new RestRequest("/instructor/register", Method.Post);

        return await ExecuteRequestAsync<InstructorDto>(request);
    }

    public async Task<RestResponse<List<InstructorDto>>> GetAllInstructorsAsync(bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue("/instructor", out RestResponse<List<InstructorDto>>? instructors)
            && instructors is not null) return instructors;

        var request = new RestRequest("/instructor");
        var response = await ExecuteRequestAsync<List<InstructorDto>>(request);

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set("/instructor", response, cacheEntryOptions);
        
        return response;
    }

    public async Task<RestResponse<InstructorDto>> GetInstructorByIdAsync(Guid instructorId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/instructor/{instructorId}", out RestResponse<InstructorDto>? instructor)
            && instructor is not null) return instructor;
        
        var request = new RestRequest($"/instructor/{instructorId}");
        var response = await ExecuteRequestAsync<InstructorDto>(request);

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/instructor/{instructorId}", response, cacheEntryOptions);

        return response;
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

    public async Task<RestResponse<List<TheoryLessonDto>>> GetTheoryLessonsAsync(Guid instructorId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/instructor/{instructorId}/theoryLesson", out RestResponse<List<TheoryLessonDto>>? theoryLessons)
            && theoryLessons is not null) return theoryLessons;

        var request = new RestRequest($"/instructor/{instructorId}/theoryLesson");
        var response = await ExecuteRequestAsync<List<TheoryLessonDto>>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/instructor/{instructorId}/theoryLesson", response, cacheEntryOptions);

        return response;
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

    public async Task<RestResponse<List<DrivingLessonDto>>> GetDrivingLessonsAsync(Guid instructorId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/instructor/{instructorId}/drivingLesson", out RestResponse<List<DrivingLessonDto>>? drivingLessons)
            && drivingLessons is not null) return drivingLessons;

        var request = new RestRequest($"/instructor/{instructorId}/drivingLesson");
        var response = await ExecuteRequestAsync<List<DrivingLessonDto>>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/instructor/{instructorId}/drivingLesson", response, cacheEntryOptions);
        
        return response;
    }
}