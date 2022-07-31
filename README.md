# Hello, Deployment Projects!

This episode: AWS .NET Deployment Projects. In this [Hello, Cloud](https://davidpallmann.hashnode.dev/hello-deployment-projects) blog series, we're covering the basics of AWS cloud services for newcomers who are .NET developers. If you love C# but are new to AWS, or to this particular service, this should give you a jumpstart.

In this post we'll introduce the deployment projects feature of AWS deployment tools for .NET CLI and use it to customize IaC for a "Hello, Cloud" .NET program. We'll do this step-by-step, making no assumptions other than familiarity with C# and Visual Studio. We're using Visual Studio 2022 and .NET 6.

## Our "Hello, Deployment Projects" Project

We will create a simple Razor website that looks up significant events for this day in history from a DynamoDB table and displays them. We'll be hosting the site in AWS App Runner. After we develop the web project, we'll generate a deployment project and modify it so the App Runner service can access the DynamoDB table. We'll be using the same configuration used in Hello, VPC Connector!, but this time we'll do it in the CDK deployment project. Our updates to the CDK will create a DynamoDB table, a VPC endpoint for DynamoDB, a VPC connector for App Runner, a policy granting access to the DynamoDB table, and modifications to the App Runner instance role. The only thing we'll do manually in the AWS console is add data records to the table.

See the blog post for the tutorial to create this project and run it on AWS.

