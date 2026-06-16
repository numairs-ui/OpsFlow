import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DataAccessOrg } from './data-access-org';

describe('DataAccessOrg', () => {
  let component: DataAccessOrg;
  let fixture: ComponentFixture<DataAccessOrg>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DataAccessOrg],
    }).compileComponents();

    fixture = TestBed.createComponent(DataAccessOrg);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
