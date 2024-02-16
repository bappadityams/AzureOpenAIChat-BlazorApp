## Authentication and Authorization in Generative AI applications with Entra ID and Azure AI Search

This sample address a scenario where a customer wishes to implement authentication and authorization for their generative AI applications. 

<img src=rag-aoai-arch.png"/>

### Setting up Azure AD Apps
Azure AD app must be registered in order to make the optional login and document level access control system work correctly.

### Configure Security Groups

You have two different options available to you on how you can further configure your application(s) to receive the groups claim.

- Receive all the groups that the signed-in user is assigned to in an Azure AD tenant, including nested groups.
- Receive the groups claim values from a filtered set of groups that your application is programmed to work with (Not available in the Azure AD Free edition)

### Index file content and metadata by using Azure AI Search





