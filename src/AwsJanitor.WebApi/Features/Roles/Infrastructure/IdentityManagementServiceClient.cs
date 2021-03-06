using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.IdentityManagement.Model;
using AwsJanitor.WebApi.Features.Roles.Model;

namespace AwsJanitor.WebApi.Features.Roles.Infrastructure.Persistence
{
    public class IdentityManagementServiceClient : IIdentityManagementServiceClient
    {
        private readonly IAmazonIdentityManagementServiceWrapper _client;

        public IdentityManagementServiceClient(IAmazonIdentityManagementServiceWrapper client)
        {
            _client = client;
        }

        public async Task DeleteRolePoliciesAsync(RoleName roleName, IEnumerable<string> namesOfPoliciesToDelete)
        {
            foreach (var policyName in namesOfPoliciesToDelete)
            {
                await _client.DeleteRolePolicyAsync(new DeleteRolePolicyRequest
                {
                    RoleName = roleName,
                    PolicyName = policyName
                });
            }
        }


        public async Task DeleteRoleAsync(RoleName roleName)
        {
            var policiesResponse =
                await _client.ListRolePoliciesAsync(new ListRolePoliciesRequest {RoleName = roleName});

            await DeleteRolePoliciesAsync(roleName, policiesResponse.PolicyNames);

            await _client.DeleteRoleAsync(new DeleteRoleRequest {RoleName = roleName});
        }

        
        public async Task<IEnumerable<Policy>> PutPoliciesAsync(RoleName roleName, IEnumerable<Policy> policies)
        {
            var policiesAdded = new List<Policy>();
            var tasks = new List<Task>();
            foreach (var policy in policies)
            {
                tasks.Add(Task.Run(async () =>
                        {
                            GetRolePolicyResponse getRolePolicyResponse;
                            try
                            {
                                getRolePolicyResponse = await _client.GetPolicyAsync(new GetRolePolicyRequest
                                    {RoleName = roleName, PolicyName = policy.Name});
                            }
                            catch (NoSuchEntityException)
                            {
                                getRolePolicyResponse = null;
                            }

                            if (
                                getRolePolicyResponse != null &&
                                policy.Document == Uri.UnescapeDataString(getRolePolicyResponse.PolicyDocument)
                            )
                            {
                                return;
                            }

                            policiesAdded.Add(policy);

                            var rolePolicyRequest = new PutRolePolicyRequest
                            {
                                RoleName = roleName,
                                PolicyName = policy.Name,
                                PolicyDocument = policy.Document
                            };
                            await _client.PutRolePolicyAsync(rolePolicyRequest);
                        }
                    )
                );
            }
            Task.WaitAll(tasks.ToArray());
            return policiesAdded;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}