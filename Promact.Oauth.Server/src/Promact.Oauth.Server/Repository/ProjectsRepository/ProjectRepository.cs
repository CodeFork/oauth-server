﻿using Promact.Oauth.Server.Data_Repository;
using Promact.Oauth.Server.Models.ApplicationClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using Promact.Oauth.Server.Models;
using AutoMapper;
using System.Threading.Tasks;

namespace Promact.Oauth.Server.Repository.ProjectsRepository
{
    public class ProjectRepository : IProjectRepository
    {
        #region "Private Variable(s)"
        private readonly IDataRepository<Project> _projectDataRepository;
        private readonly IDataRepository<ProjectUser> _projectUserDataRepository;
        private readonly IDataRepository<ApplicationUser> _userDataRepository;
        private readonly IMapper _mapperContext;
        #endregion

        #region "Constructor"
        public ProjectRepository(IDataRepository<Project> projectDataRepository, IDataRepository<ProjectUser> projectUserDataRepository, IDataRepository<ApplicationUser> userDataRepository, IMapper mapperContext)
        {
            _projectDataRepository = projectDataRepository;
            _projectUserDataRepository = projectUserDataRepository;
            _userDataRepository = userDataRepository;
            _mapperContext = mapperContext;
        }
        #endregion

        /// <summary>
        /// Get All Projects list from the database
        /// </summary>
        /// <returns></returns>List of Projects
        public async Task<IEnumerable<ProjectAc>> GetAllProjects()
        {
            var projects = _projectDataRepository.List().ToList();
            var projectAcs = new List<ProjectAc>();

            projects.ForEach(project =>
            {
                var teamLeaders = _userDataRepository.FirstOrDefault(x => x.Id == project.TeamLeaderId);
                var teamLeader = new UserAc();
                teamLeader.FirstName = teamLeaders.FirstName;
                teamLeader.LastName = teamLeaders.LastName;
                teamLeader.Email = teamLeaders.Email;
                var CreatedBy = _userDataRepository.FirstOrDefault(x => x.Id == project.CreatedBy)?.FirstName;
                var UpdatedBy = _userDataRepository.FirstOrDefault(x => x.Id == project.UpdatedBy)?.FirstName;
                string UpdatedDate;
                if (project.UpdatedDateTime == null)
                { UpdatedDate = ""; }
                else
                { UpdatedDate = project.UpdatedDateTime.ToString(); }
                var projectObject = _mapperContext.Map<Project, ProjectAc>(project);
                projectObject.TeamLeader = teamLeader;
                projectObject.CreatedBy = CreatedBy;
                projectObject.CreatedDate = project.CreatedDateTime.ToLocalTime().ToString("dd'/'MM'/'yyyy HH:mm");
                projectObject.UpdatedBy = UpdatedBy;
                projectObject.UpdatedDate = UpdatedDate;
                projectAcs.Add(projectObject);

            });
            return projectAcs;
        }

        /// <summary>
        /// Adds new project in the database
        /// </summary>
        /// <param name="newProject">project that need to be added</param>
        /// <param name="createdBy">Login User Id</param>
        /// <returns>project id of newly created project</returns>
        public async Task<int> AddProject(ProjectAc newProject, string createdBy)
        {
            var projectObject = _mapperContext.Map<ProjectAc, Project>(newProject);
            projectObject.CreatedDateTime = DateTime.UtcNow;
            projectObject.CreatedBy = createdBy;
            projectObject.ApplicationUsers = null;

            _projectDataRepository.Add(projectObject);
            return projectObject.Id;
        }

        /// <summary>
        /// Adds UserId and ProjectId in UserProject table
        /// </summary>
        /// <param name="newProjectUser"></param>ProjectId and UserId information that need to be added
        public void AddUserProject(ProjectUser newProjectUser)
        {
            _projectUserDataRepository.Add(newProjectUser);
        }

        /// <summary>
        /// Get the single project and list of users related project Id from the database(project and ProjectUser Table)
        /// </summary>
        /// <param name="id"></param>Project id that need to be featch the Project and list of users
        /// <returns></returns>Project and User/Users infromation 
        /// 
        public async Task<ProjectAc> GetById(int id)
        {
            List<UserAc> applicationUserList = new List<UserAc>();
            var project = _projectDataRepository.FirstOrDefault(x => x.Id == id);
            List<ProjectUser> projectUserList = _projectUserDataRepository.Fetch(y => y.ProjectId == project.Id).ToList();
            foreach (ProjectUser pu in projectUserList)
            {
                var applicationUser = _userDataRepository.FirstOrDefault(z => z.Id == pu.UserId);
                applicationUserList.Add(new UserAc
                {
                    Id = applicationUser.Id,
                    FirstName = applicationUser.FirstName
                });
            }

            var projectObject = _mapperContext.Map<Project, ProjectAc>(project);
            var a = _userDataRepository.FirstOrDefault(x => x.Id == project.TeamLeaderId);
            projectObject.TeamLeader = new UserAc { FirstName = a.FirstName, LastName = a.LastName, Email = a.Email };
            projectObject.ApplicationUsers = applicationUserList;
            return projectObject;
        }

        /// <summary>
        /// Update Project information and User list information In Project table and Project User Table
        /// </summary>
        /// <param name="editProject"></param>Updated information in editProject Parmeter
        /// <param name="updatedBy"></param>Login User Id
        public async Task<int> EditProject(ProjectAc editProject, string updatedBy)
        {
            var projectId = editProject.Id;
            var projectInDb = _projectDataRepository.FirstOrDefault(x => x.Id == projectId);
            projectInDb.IsActive = editProject.IsActive;
            projectInDb.Name = editProject.Name;
            projectInDb.TeamLeaderId = editProject.TeamLeaderId;
            projectInDb.SlackChannelName = editProject.SlackChannelName;
            projectInDb.UpdatedDateTime = DateTime.UtcNow;
            projectInDb.UpdatedBy = updatedBy;
            _projectDataRepository.Update(projectInDb);


            //Delete old users from project user table
            _projectUserDataRepository.Delete(x => x.ProjectId == projectId);
            _projectUserDataRepository.Save();

            editProject.ApplicationUsers.ForEach(x =>
            {
                _projectUserDataRepository.Add(new ProjectUser
                {
                    ProjectId = projectInDb.Id,
                    UpdatedDateTime = DateTime.UtcNow,
                    UpdatedBy = updatedBy,
                    CreatedBy = projectInDb.CreatedBy,
                    CreatedDateTime = projectInDb.CreatedDateTime,
                    UserId = x.Id
                });
            });
            return editProject.Id;
        }

        /// <summary>
        /// Check Project and SlackChannelName is already exists or not 
        /// </summary>
        /// <param name="project"></param> pass the project parameter
        /// <returns>projectAc object</returns>
        public ProjectAc checkDuplicateFromEditProject(ProjectAc project)
        {
            var projectName = _projectDataRepository.FirstOrDefault(x => x.Id != project.Id && x.Name == project.Name);
            var sName = _projectDataRepository.FirstOrDefault(x => x.Id != project.Id && x.SlackChannelName == project.SlackChannelName);
            if (projectName == null && sName == null)
            { return project; }
            else if (projectName != null && sName == null)
            { project.Name = null; return project; }
            else if (projectName == null && sName != null)
            { project.SlackChannelName = null; return project; }
            else
            { project.Name = null; project.SlackChannelName = null; return project; }

        }

        /// <summary>
        /// Check Project and SlackChannelName is already exists or not 
        /// </summary>
        /// <param name="project"></param> pass the project parameter
        /// <returns>projectAc object</returns>
        public ProjectAc checkDuplicate(ProjectAc project)
        {
            var projectName = _projectDataRepository.FirstOrDefault(x => x.Name == project.Name);
            var sName = _projectDataRepository.FirstOrDefault(x => x.SlackChannelName == project.SlackChannelName);
            if (projectName == null && sName == null)
            { return project; }
            else if (projectName != null && sName == null)
            { project.Name = null; return project; }
            else if (projectName == null && sName != null)
            { project.SlackChannelName = null; return project; }
            else
            { project.Name = null; project.SlackChannelName = null; return project; }
        }

        /// <summary>
        /// Fetches the project details of the given GroupName
        /// </summary>
        /// <param name="GroupName"></param>
        /// <returns>object of Project</returns>
        public ProjectAc GetProjectByGroupName(string GroupName)
        {
            try
            {
                var project = _projectDataRepository.FirstOrDefault(x => x.SlackChannelName.Equals(GroupName));
                var projectAc = new ProjectAc();
                if (project != null)
                {
                    projectAc.CreatedBy = project.CreatedBy;
                    //projectAc.CreatedDate = project.CreatedDateTime;
                    projectAc.Id = project.Id;
                    projectAc.IsActive = project.IsActive;
                    projectAc.Name = project.Name;
                    projectAc.SlackChannelName = project.SlackChannelName;
                    projectAc.TeamLeaderId = project.TeamLeaderId;
                    project.UpdatedBy = project.UpdatedBy;
                }
                return projectAc;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        /// <summary>
        /// This method is used to fetch list of users/employees of the given group name. - JJ
        /// </summary>
        /// <param name="GroupName"></param>
        /// <param name="UserName"></param>
        /// <returns>list of object of UserAc</returns>
        public List<UserAc> GetProjectUserByGroupName(string GroupName)
        {
            try
            {
                var project = _projectDataRepository.FirstOrDefault(x => x.SlackChannelName.Equals(GroupName));
                var userProjects = new List<UserAc>();
                if (project != null)
                {
                    var projectUserList = _projectUserDataRepository.Fetch(x => x.ProjectId == project.Id).ToList();
                    foreach (var projectUser in projectUserList)
                    {
                        var user = _userDataRepository.FirstOrDefault(x => x.Id == projectUser.UserId);
                        var userAc = new UserAc();
                        userAc.Id = user.Id;
                        userAc.Email = user.Email;
                        userAc.FirstName = user.FirstName;
                        userAc.IsActive = user.IsActive;
                        userAc.LastName = user.LastName;
                        userAc.UserName = user.UserName;
                        userProjects.Add(userAc);
                    }
                }
                return userProjects;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}