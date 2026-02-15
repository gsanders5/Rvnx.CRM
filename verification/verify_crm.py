from playwright.sync_api import sync_playwright

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    context = browser.new_context()
    page = context.new_page()

    try:
        # Navigate to Contacts
        page.goto("http://localhost:5000/Contacts")
        print("Navigated to Contacts")

        # Create Person A (Parent)
        page.click("text=Create New")
        page.fill('input[name="FirstName"]', "John")
        page.fill('input[name="LastName"]', "Parent")
        page.fill('input[name="Email"]', "john.parent@example.com")
        page.click('input[type="submit"]')
        print("Created John Parent")

        # Create Person B (Child)
        page.click("text=Create New")
        page.fill('input[name="FirstName"]', "Jane")
        page.fill('input[name="LastName"]', "Child")
        page.fill('input[name="Email"]', "jane.child@example.com")
        page.click('input[type="submit"]')
        print("Created Jane Child")

        # Click on John Parent
        page.click("a:text-is('John Parent')")
        print("On John Parent Details")

        # Add Relationship
        page.click("text=Add Relationship")

        # Select Jane Child
        page.select_option('select[name="RelatedPersonId"]', label="Jane Child")

        # Select Relationship Type "Parent"
        page.select_option('select[name="relationshipTypeSelection"]', label="Parent")

        # Save
        page.click('input[type="submit"]')
        print("Saved Relationship")

        # Verify on John's page
        # John is Source. Relationship is Parent/Child.
        # Display logic: OppositeName (Child).
        # Expect "Child: Jane Child".
        page.wait_for_selector("text=Child:")
        page.wait_for_selector("a:text-is('Jane Child')")
        print("Verified John has Child Jane")

        # Verify on Jane's page
        page.click("a:text-is('Jane Child')")
        print("Navigated to Jane Child")

        # Jane is Target.
        # Display logic: Name (Parent).
        # Expect "Parent: John Parent".
        page.wait_for_selector("text=Parent:")
        page.wait_for_selector("a:text-is('John Parent')")
        print("Verified Jane has Parent John")

        # Take Screenshot
        page.screenshot(path="verification/crm_relationship_verification.png", full_page=True)
        print("Screenshot taken")

    except Exception as e:
        print(f"Error: {e}")
        page.screenshot(path="verification/error.png")
        raise e
    finally:
        browser.close()

with sync_playwright() as playwright:
    run(playwright)
