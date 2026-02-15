import time
from playwright.sync_api import sync_playwright

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    context = browser.new_context()
    page = context.new_page()

    try:
        # Navigate to Home
        print("Navigating to Home...")
        page.goto("http://localhost:5000")
        time.sleep(2)

        # Take screenshot of Home (check Navbar)
        page.screenshot(path="verification_home.png")
        print("Home screenshot taken.")

        # Navigate to Contacts
        print("Navigating to Contacts...")
        page.click("text=Contacts")
        time.sleep(2)

        # Create a new contact
        print("Creating contact...")
        page.click("text=Create New")
        page.fill("input[name='FirstName']", "Test")
        page.fill("input[name='LastName']", "User")
        page.fill("input[name='Birthday']", "1990-01-01") # Should be ~35 years old
        page.click("input[value='Create']")
        time.sleep(2)

        # Go to Details of the new contact
        print("Going to Details...")
        page.click("text=Test User")
        time.sleep(2)

        # Take screenshot of Details (Check Age, hidden fields)
        page.screenshot(path="verification_details_initial.png")
        print("Details initial screenshot taken.")

        # Add Contact Info
        print("Adding Contact Info...")
        page.click("a[href*='/ContactInfos/Create']")
        time.sleep(1)

        page.fill("input[name='Type']", "Twitter")
        page.fill("input[name='Value']", "@testuser")
        page.fill("input[name='Label']", "Personal")
        page.click("input[value='Add']")
        time.sleep(2)

        # Add Fact
        print("Adding Fact...")
        page.click("a[href*='/Facts/Create']")
        time.sleep(1)

        page.fill("input[name='Category']", "Hobbies")
        page.fill("textarea[name='Value']", "Coding")
        page.click("input[value='Add']")
        time.sleep(2)

        # Add Relationship (to test UI text)
        print("Checking Relationship UI...")
        page.click("a[href*='/Relationships/Create']")
        time.sleep(1)

        # Check dropdown options text
        # Just take a screenshot of the create relationship page
        page.screenshot(path="verification_relationship_create.png")
        print("Relationship Create screenshot taken.")

        # Go back to details
        print("Going back to Details...")
        # Since we are on Create page, "Cancel" goes back to Details?
        # My code: <a asp-controller="Contacts" asp-action="Details" ...>Cancel</a>
        page.click("text=Cancel")
        time.sleep(2)

        # Take final screenshot of Details
        page.screenshot(path="verification_details_final.png")
        print("Details final screenshot taken.")

    except Exception as e:
        print(f"Error: {e}")
        page.screenshot(path="error.png")
    finally:
        browser.close()

with sync_playwright() as playwright:
    run(playwright)
