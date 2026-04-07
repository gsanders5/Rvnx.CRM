#!/bin/bash
# Review security header tests from CI failure
cat Rvnx.CRM.Tests/Security/SecurityHeadersTests.cs | grep -C 5 "SecurityHeadersTests"
