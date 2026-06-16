import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DataAccessTemplates } from './data-access-templates';

describe('DataAccessTemplates', () => {
  let component: DataAccessTemplates;
  let fixture: ComponentFixture<DataAccessTemplates>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DataAccessTemplates],
    }).compileComponents();

    fixture = TestBed.createComponent(DataAccessTemplates);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
