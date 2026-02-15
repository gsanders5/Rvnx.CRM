from playwright.sync_api import sync_playwright
import os

def test_crm_flow():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context()
        page = context.new_page()

        try:
            # 1. Go to Contacts
            print("Navigating to Contacts...")
            page.goto("http://localhost:5000/Contacts")

            # 2. Create New Contact
            print("Creating new contact...")
            page.click("text=Create New")
            page.fill("input[name='FirstName']", "John")
            page.fill("input[name='LastName']", "Doe")
            page.fill("input[name='Email']", "john.doe@example.com")
            page.click("input[value='Create']")

            # Wait for navigation back to Index
            page.wait_for_url("http://localhost:5000/Contacts")
            print("Contact created.")

            # 3. Go to Details
            print("Navigating to Details...")
            # Assuming it's the first/latest contact
            page.click("text=John Doe")

            # 4. Add Pet
            print("Adding Pet...")
            # The Pets Add button has href containing "Pets/Create".
            page.click("a[href*='Pets/Create']")

            page.fill("input[name='Name']", "Rex")
            page.fill("input[name='Species']", "Dog")
            page.fill("input[name='Breed']", "German Shepherd")
            page.click("input[value='Add Pet']")

            # Wait for navigation back to Details
            page.wait_for_selector("text=Rex")
            print("Pet added.")

            # 5. Take Screenshot
            print("Taking screenshot...")
            if not os.path.exists("/home/jules/verification"):
                os.makedirs("/home/jules/verification")
            page.screenshot(path="/home/jules/verification/contact_details_with_pet.png", full_page=True)
            print("Screenshot saved.")

        except Exception as e:
            print(f"Error: {e}")
            page.screenshot(path="/home/jules/verification/error.png")
            raise e
        finally:
            browser.close()

if __name__ == "__main__":
    test_crm_flow()
